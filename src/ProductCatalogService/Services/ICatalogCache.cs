namespace ProductCatalogService.Services;

public interface ICatalogCache
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value, TimeSpan ttl);
    Task RemoveAsync(string key);
}
