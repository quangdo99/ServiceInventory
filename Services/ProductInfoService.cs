using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ServiceInventory.Models;
using System.Text.Json;

namespace ServiceInventory.Services
{
    public class ProductInfoService
    {
        private readonly IMongoCollection<ProductInfo> _productInfoCollection;

        public ProductInfoService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionURI);
            IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _productInfoCollection = database.GetCollection<ProductInfo>(mongoDBSettings.Value.CollectionProductInfo);
        }

        public ProductInfo GetProductInfo(string productCode)
        {
            var filter = Builders<ProductInfo>.Filter.Eq("code", productCode);
            return _productInfoCollection.Find(filter).FirstOrDefault();
        }
    }
}
