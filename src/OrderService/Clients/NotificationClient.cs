namespace OrderService.Clients;

public record NotifyDto(int OrderId, int UserId, string Status, string Message);

public interface INotificationClient
{
    Task NotifyAsync(NotifyDto dto);
}

/// <summary>
/// Typed HttpClient that tells NotificationService to inform the customer.
/// </summary>
public class NotificationClient : INotificationClient
{
    private readonly HttpClient _http;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient http, ILogger<NotificationClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task NotifyAsync(NotifyDto dto)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/notifications", dto);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            // A failed notification should not fail the whole order — just log it.
            _logger.LogError(ex, "Failed to send notification for order {OrderId}", dto.OrderId);
        }
    }
}
