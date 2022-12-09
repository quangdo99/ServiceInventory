using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ServiceInventory.Models
{
    public class ProductItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string product_code { get; set; }

        public string code { get; set; }
        public int status { get; set; }
        public ProductItem(string product_code, string code, int status)
        {
            this.product_code = product_code;
            this.code = code;
            this.status = status;
        }
    }
}
