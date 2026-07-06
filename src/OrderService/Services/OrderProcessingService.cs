using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Messaging;
using OrderService.Models;

namespace OrderService.Services;

public interface IOrderProcessingService
{
    Task<OrderResponseDto> CreateOrderAsync(OrderCreateDto dto);
    Task<OrderResponseDto?> GetByIdAsync(int id);
    Task<List<OrderResponseDto>> GetAllAsync();
}

/// <summary>
/// Phase 4 — the ASYNCHRONOUS order flow.
///
/// Placing an order no longer calls InventoryService/NotificationService over HTTP and waits.
/// Instead we:
///   1. price the items by reading the catalog (a query — kept synchronous),
///   2. save the order as "Pending",
///   3. publish an "OrderPlaced" event and return immediately.
///
/// The rest of the saga happens in the background (see OrderSagaConsumer): inventory reserves
/// stock and answers with an event, which flips the order to Confirmed or Cancelled.
/// This decouples the services — OrderService doesn't care who reserves stock or when.
/// </summary>
public class OrderProcessingService : IOrderProcessingService
{
    private readonly OrdersDbContext _db;
    private readonly ICatalogClient _catalog;
    private readonly RabbitMqEventBus _bus;
    private readonly ILogger<OrderProcessingService> _logger;

    public OrderProcessingService(
        OrdersDbContext db,
        ICatalogClient catalog,
        RabbitMqEventBus bus,
        ILogger<OrderProcessingService> logger)
    {
        _db = db;
        _catalog = catalog;
        _bus = bus;
        _logger = logger;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(OrderCreateDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            throw new ArgumentException("An order must contain at least one item.");

        var order = new Order { UserId = dto.UserId, ShippingAddress = dto.ShippingAddress, Status = "Pending" };
        foreach (var line in dto.Items)
        {
            var product = await _catalog.GetProductAsync(line.ProductId)
                ?? throw new ArgumentException($"Product '{line.ProductId}' does not exist in the catalog.");

            var subtotal = product.Price * line.Quantity;
            order.OrderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = line.Quantity,
                Subtotal = subtotal
            });
            order.TotalAmount += subtotal;
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Start the saga. The correlation id lets us trace this one order across every service.
        var correlationId = Guid.NewGuid().ToString("N");
        _bus.Publish("OrderPlaced",
            new OrderPlacedMessage(order.Id, order.UserId,
                order.OrderItems.Select(i => new SagaItem(i.ProductId, i.Quantity)).ToList()),
            correlationId);

        _logger.LogInformation("[{CorrelationId}] Order {OrderId} placed (Pending); OrderPlaced published",
            correlationId, order.Id);

        return MapToDto(order); // returns immediately with status "Pending"
    }

    public async Task<OrderResponseDto?> GetByIdAsync(int id)
    {
        var order = await _db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
        return order == null ? null : MapToDto(order);
    }

    public async Task<List<OrderResponseDto>> GetAllAsync()
    {
        var orders = await _db.Orders.Include(o => o.OrderItems).ToListAsync();
        return orders.Select(MapToDto).ToList();
    }

    private static OrderResponseDto MapToDto(Order order) => new(
        order.Id, order.UserId, order.Status, order.TotalAmount, order.ShippingAddress, order.OrderDate,
        order.OrderItems.Select(i =>
            new OrderItemResponseDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice, i.Subtotal)).ToList());
}
