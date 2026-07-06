using MongoDB.Bson.Serialization.Attributes;

namespace ProductCatalogService.Models;

/// <summary>
/// A product stored as a MongoDB document.
///
/// Why a document (and not a SQL row)? Different categories need different fields:
/// a shirt has Size + Color, a book has Author + Pages. Instead of a wide table full
/// of NULL columns, each document carries only the attributes it needs — inside the
/// flexible <see cref="Attributes"/> bag. This is the core reason a catalog fits the
/// document model.
/// </summary>
public class Product
{
    // We use a human-readable string as the Mongo _id (e.g. "tshirt", "book").
    // Mongo allows any type for _id, and a stable, readable id makes it trivial for the
    // other services (Inventory, Order) to reference the same product and easy to demo.
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // The catalog owns product info only. Stock lives in InventoryService — NOT here.
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    // The flexible part: category-specific attributes (e.g. {"Size":"L","Color":"Blue"}).
    // In SQL this would force NULL-heavy columns or extra tables; in Mongo it is just a sub-document.
    public Dictionary<string, string> Attributes { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
