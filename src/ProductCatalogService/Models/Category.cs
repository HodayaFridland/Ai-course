using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ProductCatalogService.Models;

/// <summary>
/// A product category, stored as its own MongoDB document.
/// </summary>
public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
