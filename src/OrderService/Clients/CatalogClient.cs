namespace OrderService.Clients;

// We only care about a few fields of the catalog's product for pricing an order.
public record CatalogProductDto(string Id, string Name, decimal Price);

public interface ICatalogClient
{
    Task<CatalogProductDto?> GetProductAsync(string productId);
}

/// <summary>
/// Typed HttpClient that calls ProductCatalogService over HTTP.
/// The base address is injected in Program.cs from configuration, so this class
/// never hard-codes where the catalog lives.
/// </summary>
public class CatalogClient : ICatalogClient
{
    private readonly HttpClient _http;

    public CatalogClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<CatalogProductDto?> GetProductAsync(string productId)
    {
        var response = await _http.GetAsync($"/api/products/{productId}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CatalogProductDto>();
    }
}
