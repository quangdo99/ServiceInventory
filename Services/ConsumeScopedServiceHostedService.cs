using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ServiceInventory.Models;
using System.Text.Json;

namespace ServiceInventory.Services
{
    internal interface IScopedProcessingService
    {
        Task DoWork(CancellationToken stoppingToken, IMongoCollection<ProductInfo> _productInfoCollection, ServiceBusReceiver _serviceBusReceiver);
    }

    internal class ScopedProcessingService : IScopedProcessingService
    {
        private int executionCount = 0;
        private readonly ILogger _logger;
        //private IMongoCollection<ProductInfo> _productInfoCollection;

        public ScopedProcessingService(ILogger<ScopedProcessingService> logger)
        {
            _logger = logger;
        }

        public async Task DoWork(CancellationToken stoppingToken, IMongoCollection<ProductInfo> productInfoCollection, ServiceBusReceiver _serviceBusReceiver)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                executionCount++;

                _logger.LogInformation(
                    "Scoped Processing Service is working. Count: {Count}", executionCount);
                var message = await _serviceBusReceiver.ReceiveMessagesAsync(1);
                if (message != null && message.Count() > 0)
                {
                    var productInfo = JsonSerializer.Deserialize<ProductInfo>(message.FirstOrDefault().Body);
                    if (productInfo != null)
                    {
                        _logger.LogInformation($"Received: {message.FirstOrDefault().Body}");
                        if (productInfo.status == 0)
                        {
                            await productInfoCollection.InsertOneAsync(productInfo);
                        }
                        if (productInfo.status == 1)
                        {
                            FilterDefinition<ProductInfo> filter = Builders<ProductInfo>.Filter.Eq("code", productInfo.code);
                            UpdateDefinition<ProductInfo> update = Builders<ProductInfo>.Update
                                .Set("code", productInfo.code)
                                .Set("status", productInfo.status);
                            await productInfoCollection.UpdateOneAsync(filter, update);
                        }
                    }
                    await _serviceBusReceiver.CompleteMessageAsync(message.FirstOrDefault());
                    _logger.LogInformation("Message Receiver Commited");
                } else
                {
                    _logger.LogInformation("Message Receiver Not Found");
                }
                await Task.Delay(1000, stoppingToken);
            }

            //_logger.LogInformation(
            //        "Scoped Processing Service is working. Count: {Count}", executionCount);
            //var message = await _serviceBusReceiver.ReceiveMessagesAsync(1);
            //if (message != null)
            //{
            //    var productInfo = JsonSerializer.Deserialize<ProductInfo>(message.FirstOrDefault().Body);
            //    if (productInfo != null)
            //    {
            //        _logger.LogInformation($"Received: {message.FirstOrDefault().Body}");
            //        if (productInfo.status == 0)
            //        {
            //            await productInfoCollection.InsertOneAsync(productInfo);
            //        }
            //        if (productInfo.status == 1)
            //        {
            //            FilterDefinition<ProductInfo> filter = Builders<ProductInfo>.Filter.Eq("code", productInfo.code);
            //            UpdateDefinition<ProductInfo> update = Builders<ProductInfo>.Update
            //                .Set("code", productInfo.code)
            //                .Set("status", productInfo.status);
            //            await productInfoCollection.UpdateOneAsync(filter, update);
            //        }
            //    }
            //    await _serviceBusReceiver.CompleteMessageAsync(message.FirstOrDefault());
            //}

            //_productInfoCollection = productInfoCollection;
            //try
            //{
            //    // add handler to process messages
            //    _serviceBusProcessor.ProcessMessageAsync += MessageHandler;

            //    // add handler to process any errors
            //    _serviceBusProcessor.ProcessErrorAsync += ErrorHandler;

            //    // start processing 
            //    //await _serviceBusProcessor.StartProcessingAsync();

            //    Console.WriteLine("Wait for a minute and then press any key to end the processing");
            //    Console.ReadKey();

            //    // stop processing 
            //    Console.WriteLine("\nStopping the receiver...");
            //    //await _serviceBusProcessor.StopProcessingAsync();
            //    Console.WriteLine("Stopped receiving messages");
            //}
            //finally
            //{
            //    // Calling DisposeAsync on client types is required to ensure that network
            //    // resources and other unmanaged objects are properly cleaned up.
            //    await _serviceBusProcessor.DisposeAsync();
            //    await _clientMsgBus.DisposeAsync();
            //}
        }

        //// handle received messages
        //async Task MessageHandler(ProcessMessageEventArgs args)
        //{
        //    string body = args.Message.Body.ToString();
        //    Console.WriteLine($"Received: {body}");

        //    var productInfo = JsonSerializer.Deserialize<ProductInfo>(body);
        //    if (productInfo != null)
        //    {
        //        if (productInfo.status == 0)
        //        {
        //            await _productInfoCollection.InsertOneAsync(productInfo);
        //        }
        //        if (productInfo.status == 1)
        //        {
        //            FilterDefinition<ProductInfo> filter = Builders<ProductInfo>.Filter.Eq("code", productInfo.code);
        //            UpdateDefinition<ProductInfo> update = Builders<ProductInfo>.Update
        //                .Set("code", productInfo.code)
        //                .Set("status", productInfo.status);
        //            await _productInfoCollection.UpdateOneAsync(filter, update);
        //        }
        //    }

        //    // complete the message. message is deleted from the queue. 
        //    await args.CompleteMessageAsync(args.Message);
        //}

        //// handle any errors when receiving messages
        //Task ErrorHandler(ProcessErrorEventArgs args)
        //{
        //    Console.WriteLine(args.Exception.ToString());
        //    return Task.CompletedTask;
        //}
    }

    public class ConsumeScopedServiceHostedService : BackgroundService
    {
        private readonly ILogger<ConsumeScopedServiceHostedService> _logger;
        private readonly IMongoCollection<ProductInfo> _productInfoCollection;
        private readonly ServiceBusClient _clientMsgBus;
        private readonly ServiceBusReceiver _serviceBusReceiver;

        public ConsumeScopedServiceHostedService(IServiceProvider services,
            ILogger<ConsumeScopedServiceHostedService> logger, IOptions<MongoDBSettings> mongoDBSettings, IOptions<MessageBus> messageBusSettings)
        {
            Services = services;
            _logger = logger;

            MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionURI);
            IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _productInfoCollection = database.GetCollection<ProductInfo>(mongoDBSettings.Value.CollectionProductInfo);

            // TODO: Replace the <NAMESPACE-CONNECTION-STRING> and <QUEUE-NAME> placeholders
            //var clientOptions = new ServiceBusClientOptions()
            //{
            //    TransportType = ServiceBusTransportType.AmqpWebSockets
            //};
            //_clientMsgBus = new ServiceBusClient(messageBusSettings.Value.ConnectionString, clientOptions);
            //_serviceBusProcessor = _clientMsgBus.CreateProcessor(messageBusSettings.Value.QueueName, new ServiceBusProcessorOptions());
            _clientMsgBus = new ServiceBusClient(messageBusSettings.Value.ConnectionString);
            _serviceBusReceiver = _clientMsgBus.CreateReceiver(messageBusSettings.Value.QueueName);
        }

        public IServiceProvider Services { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service running.");

            await DoWork(stoppingToken);
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is working.");

            using (var scope = Services.CreateScope())
            {
                var scopedProcessingService =
                    scope.ServiceProvider
                        .GetRequiredService<IScopedProcessingService>();

                await scopedProcessingService.DoWork(stoppingToken, _productInfoCollection, _serviceBusReceiver);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Consume Scoped Service Hosted Service is stopping.");

            await base.StopAsync(stoppingToken);
        }
    }
}
