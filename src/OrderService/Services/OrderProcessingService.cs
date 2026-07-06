using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;

namespace OrderService.Services;

public interface IOrderProcessingService
{
    Task<OrderResponseDto> CreateOrderAsync(OrderCreateDto dto);
    Task<OrderResponseDto?> GetByIdAsync(int id);
    Task<List<OrderResponseDto>> GetAllAsync();
}

/// <summary>
/// The synchronous order flow for Phase 2. It coordinates three other services over HTTP:
///   1. ProductCatalogService — to price each line item
///   2. InventoryService      — to reserve stock (or find out it's unavailable)
///   3. NotificationService   — to tell the customer the result
///
/// In Phase 4 this same flow becomes ASYNCHRONOUS (events over RabbitMQ), which is the whole
/// point of the exercise — so keep this file in mind as the "before" picture.
/// </summary>
public class OrderProcessingService : IOrderProcessingService
{
    private readonly OrdersDbContext _db;
    private readonly ICatalogClient _catalog;
    private readonly IInventoryClient _inventory;
    private readonly INotificationClient _notifications;
    private readonly ILogger<OrderProcessingService> _logger;

    public OrderProcessingService(
        OrdersDbContext db,
        ICatalogClient catalog,
        IInventoryClient inventory,
        INotificationClient notifications,
        ILogger<OrderProcessingService> logger)
    {
        _db = db;
        _catalog = catalog;
        _inventory = inventory;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<OrderResponseDto> CreateOrderAsync(OrderCreateDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            throw new ArgumentException("An order must contain at least one item.");

        // 1) Price each line from the catalog (and snapshot name + price into the order).
        var order = new Order { UserId = dto.UserId, ShippingAddress = dto.ShippingAddress };
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

        // 2) Save the order as Pending so it has an Id we can reserve against.
        order.Status = "Pending";
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderId} created as Pending (total {Total}).", order.Id, order.TotalAmount);

        // 3) Try to reserve stock for the whole order.
        var reserveRequest = new ReserveRequestDto(
            order.Id,
            order.OrderItems.Select(i => new ReserveItemDto(i.ProductId, i.Quantity)).ToList());

        var reservation = await _inventory.ReserveAsync(reserveRequest);

        // 4) Confirm or cancel, then notify the customer.
        if (reservation.Success)
        {
            order.Status = "Confirmed";
            await _db.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} CONFIRMED.", order.Id);
            await _notifications.NotifyAsync(new NotifyDto(order.Id, order.UserId, "Confirmed",
                $"Your order #{order.Id} was confirmed."));
        }
        else
        {
            order.Status = "Cancelled";
            await _db.SaveChangesAsync();
            _logger.LogWarning("Order {OrderId} CANCELLED: {Reason}", order.Id, reservation.Reason);
            await _notifications.NotifyAsync(new NotifyDto(order.Id, order.UserId, "Rejected",
                $"Your order #{order.Id} was rejected: {reservation.Reason}"));
        }

        return MapToDto(order);
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
