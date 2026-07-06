using MongoDB.Driver;
using ProductCatalogService.Data;
using ProductCatalogService.Models;

namespace ProductCatalogService.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(string id);
    Task<List<Product>> GetByCategoryAsync(string categoryId);
    Task<Product> CreateAsync(Product product);
    Task<bool> UpdateAsync(string id, Product product);
    Task<bool> DeleteAsync(string id);
}

/// <summary>
/// Data access for products, backed by MongoDB.
/// Every method here maps to a simple Mongo operation (Find / InsertOne / ReplaceOne / DeleteOne).
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly IMongoCollection<Product> _products;

    public ProductRepository(MongoContext context)
    {
        _products = context.Products;
    }

    // Filter.Empty means "match everything" — like SELECT * FROM products.
    public async Task<List<Product>> GetAllAsync() =>
        await _products.Find(Builders<Product>.Filter.Empty).ToListAsync();

    public async Task<Product?> GetByIdAsync(string id) =>
        await _products.Find(p => p.Id == id).FirstOrDefaultAsync();

    public async Task<List<Product>> GetByCategoryAsync(string categoryId) =>
        await _products.Find(p => p.CategoryId == categoryId).ToListAsync();

    public async Task<Product> CreateAsync(Product product)
    {
        // If the caller didn't provide a readable id, generate a short unique one.
        if (string.IsNullOrWhiteSpace(product.Id))
            product.Id = Guid.NewGuid().ToString("N")[..8];

        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        await _products.InsertOneAsync(product);
        return product;
    }

    public async Task<bool> UpdateAsync(string id, Product product)
    {
        product.Id = id;
        product.UpdatedAt = DateTime.UtcNow;
        // ReplaceOne swaps the whole document. ModifiedCount > 0 means a document actually changed.
        var result = await _products.ReplaceOneAsync(p => p.Id == id, product);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _products.DeleteOneAsync(p => p.Id == id);
        return result.DeletedCount > 0;
    }
}
