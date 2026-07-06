using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProductCatalogService.Models;

namespace ProductCatalogService.Data;

/// <summary>
/// Central access point to the MongoDB collections.
/// Registered as a singleton — the Mongo driver manages its own connection pool internally,
/// so one shared client is the recommended pattern (unlike EF's per-request DbContext).
/// </summary>
public class MongoContext
{
    public IMongoCollection<Product> Products { get; }
    public IMongoCollection<Category> Categories { get; }

    public MongoContext(IOptions<CatalogDbSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);

        Products = database.GetCollection<Product>(settings.ProductsCollectionName);
        Categories = database.GetCollection<Category>(settings.CategoriesCollectionName);
    }
}
