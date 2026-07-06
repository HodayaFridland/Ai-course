namespace ProductCatalogService.DTOs;

/// <summary>
/// What the client sends when creating or updating a product.
/// We keep it separate from the Model so the client can't set Id/CreatedAt directly.
/// </summary>
public class ProductCreateDto
{
    // Optional readable id (e.g. "tshirt"). If left empty, the service generates one.
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
}
