using MongoDB.Driver;
using ProductCatalogService.Data;
using ProductCatalogService.Models;

namespace ProductCatalogService.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(string id);
    Task<Category> CreateAsync(Category category);
}

/// <summary>
/// Data access for categories, backed by MongoDB.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly IMongoCollection<Category> _categories;

    public CategoryRepository(MongoContext context)
    {
        _categories = context.Categories;
    }

    public async Task<List<Category>> GetAllAsync() =>
        await _categories.Find(Builders<Category>.Filter.Empty).ToListAsync();

    public async Task<Category?> GetByIdAsync(string id) =>
        await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();

    public async Task<Category> CreateAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        await _categories.InsertOneAsync(category);
        return category;
    }
}
