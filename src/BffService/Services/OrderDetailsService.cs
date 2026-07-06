using System.Net.Http.Json;

namespace BffService.Services;

public interface IOrderDetailsService
{
    Task<object?> GetOrderDetailsAsync(int id);
}

/// <summary>
/// The BFF's whole job: aggregate data from MORE THAN ONE service into a single
/// response shaped for the web client. Here we combine:
///   1) the order itself            -> OrderService
///   2) live product details        -> ProductCatalogService
/// so the client gets one round-trip instead of calling two services itself.
/// </summary>
public class OrderDetailsService : IOrderDetailsService
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<OrderDetailsService> _logger;

    public OrderDetailsService(IHttpClientFactory factory, ILogger<OrderDetailsService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<object?> GetOrderDetailsAsync(int id)
    {
        var ordersClient = _factory.CreateClient("orders");
        var catalogClient = _factory.CreateClient("catalog");

        // 1) fetch the order (service #1)
        var order = await ordersClient.GetFromJsonAsync<OrderDto>($"api/orders/{id}");
        if (order is null) return null;

        // 2) enrich each item with live catalog data (service #2)
        var enrichedItems = new List<object>();
        foreach (var item in order.Items)
        {
            ProductDto? product = null;
            try
            {
                product = await catalogClient.GetFromJsonAsync<ProductDto>($"api/products/{item.ProductId}");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Catalog lookup failed for product {ProductId}", item.ProductId);
            }

            enrichedItems.Add(new
            {
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.Subtotal,
                catalog = product is null ? null : new
                {
                    product.Description,
                    product.CategoryName,
                    currentPrice = product.Price,   // catalog price now (may differ from order-time price)
                    product.Attributes
                }
            });
        }

        // one combined view for the client
        return new
        {
            order.Id,
            order.UserId,
            order.Status,
            order.TotalAmount,
            order.ShippingAddress,
            order.OrderDate,
            Items = enrichedItems,
            aggregatedBy = "BffService (OrderService + ProductCatalogService)"
        };
    }
}

// Minimal DTOs used only to read the two services' JSON (Web defaults = case-insensitive).
public record OrderItemDto(string ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);
public record OrderDto(int Id, int UserId, string Status, decimal TotalAmount, string ShippingAddress, DateTime OrderDate, List<OrderItemDto> Items);
public record ProductDto(string Id, string Name, string Description, decimal Price, string CategoryName, Dictionary<string, string> Attributes);
