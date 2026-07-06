namespace ProductCatalogService.Services;

public class InMemoryCatalogCache : ICatalogCache
{
    private readonly Dictionary<string, (string Value, DateTimeOffset ExpiresAt)> _entries = new();
    private readonly object _sync = new();

    public Task<string?> GetAsync(string key)
    {
        lock (_sync)
        {
            if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                return Task.FromResult<string?>(entry.Value);
            }

            _entries.Remove(key);
            return Task.FromResult<string?>(null);
        }
    }

    public Task SetAsync(string key, string value, TimeSpan ttl)
    {
        lock (_sync)
        {
            _entries[key] = (value, DateTimeOffset.UtcNow.Add(ttl));
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        lock (_sync)
        {
            _entries.Remove(key);
        }

        return Task.CompletedTask;
    }
}
