using StackExchange.Redis;

namespace ProductCatalogService.Services;

/// <summary>
/// A distributed cache backed by Redis (a NoSQL key-value store) — the real implementation of
/// ICatalogCache for Phase 4. Because it's a separate server, ALL catalog replicas share the same
/// cache: a value written by catalogservice-1 is a hit for catalogservice-2. That's the whole point
/// of a distributed cache over an in-process dictionary.
/// </summary>
public class RedisCatalogCache : ICatalogCache
{
    private readonly IDatabase _db;

    public RedisCatalogCache(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<string?> GetAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public Task SetAsync(string key, string value, TimeSpan ttl) =>
        _db.StringSetAsync(key, value, ttl);

    public Task RemoveAsync(string key) =>
        _db.KeyDeleteAsync(key);
}
