using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ServiceInventory.Models;
using System.Diagnostics;
using System.Text.Json;

namespace ServiceInventory.Services
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<TimedHostedService> _logger;
        private readonly IMongoCollection<ProductInfo> _productInfoCollection;
        private readonly ServiceBusClient _clientMsgBus;
        private readonly ServiceBusProcessor _serviceBusProcessor;
        private Timer? _timer = null;

        public TimedHostedService(ILogger<TimedHostedService> logger, IOptions<MongoDBSettings> mongoDBSettings, IOptions<MessageBus> messageBusSettings)
        {
            _logger = logger;

            MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionURI);
            IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _productInfoCollection = database.GetCollection<ProductInfo>(mongoDBSettings.Value.CollectionProductInfo);

            // TODO: Replace the <NAMESPACE-CONNECTION-STRING> and <QUEUE-NAME> placeholders
            var clientOptions = new ServiceBusClientOptions()
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets
            };
            _clientMsgBus = new ServiceBusClient(messageBusSettings.Value.ConnectionString, clientOptions);
            _serviceBusProcessor = _clientMsgBus.CreateProcessor(messageBusSettings.Value.QueueName, new ServiceBusProcessorOptions());
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(5));
            //DoWork(null);

            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);

            try
            {
                // add handler to process messages
                _serviceBusProcessor.ProcessMessageAsync += MessageHandler;

                // add handler to process any errors
                _serviceBusProcessor.ProcessErrorAsync += ErrorHandler;

                // start processing 
                await _serviceBusProcessor.StartProcessingAsync();

                Console.WriteLine("Wait for a minute and then press any key to end the processing");
                Console.ReadKey();

                // stop processing 
                Console.WriteLine("\nStopping the receiver...");
                await _serviceBusProcessor.StopProcessingAsync();
                Console.WriteLine("Stopped receiving messages");
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await _serviceBusProcessor.DisposeAsync();
                await _clientMsgBus.DisposeAsync();
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        // handle received messages
        async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            Console.WriteLine($"Received: {body}");

            var productInfo = JsonSerializer.Deserialize<ProductInfo>(body);
            if (productInfo != null)
            {
                if (productInfo.status == 0)
                {
                    await _productInfoCollection.InsertOneAsync(productInfo);
                }
                if (productInfo.status == 1)
                {
                    FilterDefinition<ProductInfo> filter = Builders<ProductInfo>.Filter.Eq("code", productInfo.code);
                    UpdateDefinition<ProductInfo> update = Builders<ProductInfo>.Update
                        .Set("code", productInfo.code)
                        .Set("status", productInfo.status);
                    await _productInfoCollection.UpdateOneAsync(filter, update);
                }
            }

            // complete the message. message is deleted from the queue. 
            await args.CompleteMessageAsync(args.Message);
        }

        // handle any errors when receiving messages
        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
    }
}
