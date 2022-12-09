using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ServiceInventory.Models
{
    public class ProductInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string code { get; set; }

        public int status { get; set; }

        public int price { get; set; }

    }
}
