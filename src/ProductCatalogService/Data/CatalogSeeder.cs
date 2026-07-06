using MongoDB.Driver;
using ProductCatalogService.Models;

namespace ProductCatalogService.Data;

/// <summary>
/// Inserts a few sample products the first time the service starts (only if the collection is empty).
/// Notice how each product carries DIFFERENT attributes — that is the document model's whole point.
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(MongoContext context, ILogger logger)
    {
        var existing = await context.Products.CountDocumentsAsync(Builders<Product>.Filter.Empty);
        if (existing > 0)
        {
            logger.LogInformation("Catalog already has {Count} products — skipping seed.", existing);
            return;
        }

        var products = new List<Product>
        {
            new Product
            {
                Id = "tshirt",
                Name = "Classic T-Shirt", Description = "100% cotton", Price = 59.90m,
                CategoryId = "clothing", CategoryName = "Clothing",
                Attributes = new() { ["Size"] = "L", ["Color"] = "Blue", ["Material"] = "Cotton" }
            },
            new Product
            {
                Id = "book",
                Name = "The Pragmatic Programmer", Description = "Software craftsmanship classic", Price = 120.00m,
                CategoryId = "books", CategoryName = "Books",
                Attributes = new() { ["Author"] = "Hunt & Thomas", ["Pages"] = "352", ["Language"] = "English" }
            },
            new Product
            {
                Id = "mouse",
                Name = "Wireless Mouse", Description = "Ergonomic 2.4GHz mouse", Price = 89.00m,
                CategoryId = "electronics", CategoryName = "Electronics",
                Attributes = new() { ["Connectivity"] = "Wireless", ["Battery"] = "AA", ["Warranty"] = "24 months" }
            }
        };

        await context.Products.InsertManyAsync(products);
        logger.LogInformation("Seeded {Count} sample products into the catalog.", products.Count);
    }
}
