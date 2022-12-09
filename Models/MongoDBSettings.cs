namespace ServiceInventory.Models;

public class MongoDBSettings {
    public string ConnectionURI { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public string CollectionProductInfo { get; set; } = null!;
    public string CollectionProductItem { get; set; } = null!;
}