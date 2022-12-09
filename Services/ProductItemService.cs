using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ServiceInventory.Models;
using System.Text.Json;

namespace ServiceInventory.Services
{
    public class ProductItemService
    {
        private readonly IMongoCollection<ProductItem> _productItemCollection;

        public ProductItemService(IOptions<MongoDBSettings> mongoDBSettings)
        {
            MongoClient client = new MongoClient(mongoDBSettings.Value.ConnectionURI);
            IMongoDatabase database = client.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _productItemCollection = database.GetCollection<ProductItem>(mongoDBSettings.Value.CollectionProductItem);
        }

        public async Task<List<ProductItem>> GetAsync(string productCode)
        {
            var filter = Builders<ProductItem>.Filter.Eq("product_code", productCode);
            return await _productItemCollection.Find(filter).ToListAsync();
        }

        public async Task CreateAsync(ProductInfo productCode, [FromBody] List<string> codes)
        {
            List<ProductItem> products = new List<ProductItem>();
            foreach (var code in codes)
            {
                products.Add(new ProductItem(productCode.code, code, 0));
            }
            await _productItemCollection.InsertManyAsync(products);
            return;
        }

        public ProductItem GetProductItem(string productCode)
        {
            var filter = Builders<ProductItem>.Filter.Eq("product_code", productCode);
            var filter2 = Builders<ProductItem>.Filter.Eq("status", 0);
            return _productItemCollection.Find(filter & filter2).FirstOrDefault();
        }

        public async Task UpdateStatus(string id)
        {
            FilterDefinition<ProductItem> filter = Builders<ProductItem>.Filter.Eq("Id", id);
            UpdateDefinition<ProductItem> update = Builders<ProductItem>.Update.Set("status", 1);
            await _productItemCollection.UpdateOneAsync(filter, update);
            return;
        }
    }
}
