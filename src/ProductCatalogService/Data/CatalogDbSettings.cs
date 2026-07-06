namespace ProductCatalogService.Data;

/// <summary>
/// Strongly-typed settings bound from appsettings.json ("CatalogDatabase" section).
/// Keeps the Mongo connection string / names out of the code.
/// </summary>
public class CatalogDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ProductsCollectionName { get; set; } = "products";
    public string CategoriesCollectionName { get; set; } = "categories";
}
