namespace OrderService.Clients;

public record ReserveItemDto(string ProductId, int Quantity);
public record ReserveRequestDto(int OrderId, List<ReserveItemDto> Items);
public record ReserveResultDto(bool Success, string? Reason);

public interface IInventoryClient
{
    Task<ReserveResultDto> ReserveAsync(ReserveRequestDto request);
    Task ReleaseAsync(ReserveRequestDto request);
}

/// <summary>
/// Typed HttpClient that calls InventoryService to reserve or release stock.
/// </summary>
public class InventoryClient : IInventoryClient
{
    private readonly HttpClient _http;

    public InventoryClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ReserveResultDto> ReserveAsync(ReserveRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("/api/inventory/reserve", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReserveResultDto>()
               ?? new ReserveResultDto(false, "No response from inventory.");
    }

    public async Task ReleaseAsync(ReserveRequestDto request)
    {
        var response = await _http.PostAsJsonAsync("/api/inventory/release", request);
        response.EnsureSuccessStatusCode();
    }
}
