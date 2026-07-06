using System.Text.Json;
using ProductCatalogService.Data;
using ProductCatalogService.Models;
using ProductCatalogService.Services;

namespace ProductCatalogService.Repositories;

public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _inner;
    private readonly ICatalogCache _cache;
    private readonly ILogger<CachedProductRepository> _logger;

    public CachedProductRepository(IProductRepository inner, ICatalogCache cache, ILogger<CachedProductRepository> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        var cacheKey = "products:all";
        var cached = await _cache.GetAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogInformation("Cache HIT for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<List<Product>>(cached) ?? new List<Product>();
        }

        _logger.LogInformation("Cache MISS for {CacheKey} — reading from MongoDB and caching", cacheKey);
        var products = await _inner.GetAllAsync();
        await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(products), TimeSpan.FromMinutes(5));
        return products;
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        var cacheKey = $"product:{id}";
        var cached = await _cache.GetAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            _logger.LogInformation("Cache HIT for {CacheKey}", cacheKey);
            return JsonSerializer.Deserialize<Product>(cached);
        }

        _logger.LogInformation("Cache MISS for {CacheKey} — reading from MongoDB and caching", cacheKey);
        var product = await _inner.GetByIdAsync(id);
        if (product != null)
        {
            await _cache.SetAsync(cacheKey, JsonSerializer.Serialize(product), TimeSpan.FromMinutes(5));
        }

        return product;
    }

    public Task<List<Product>> GetByCategoryAsync(string categoryId) => _inner.GetByCategoryAsync(categoryId);

    public async Task<Product> CreateAsync(Product product)
    {
        var created = await _inner.CreateAsync(product);
        await _cache.RemoveAsync("products:all");
        return created;
    }

    public async Task<bool> UpdateAsync(string id, Product product)
    {
        var updated = await _inner.UpdateAsync(id, product);
        if (updated)
        {
            await _cache.RemoveAsync("products:all");
            await _cache.RemoveAsync($"product:{id}");
        }

        return updated;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var deleted = await _inner.DeleteAsync(id);
        if (deleted)
        {
            await _cache.RemoveAsync("products:all");
            await _cache.RemoveAsync($"product:{id}");
        }

        return deleted;
    }
}
