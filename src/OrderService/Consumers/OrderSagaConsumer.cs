using System.Text.Json;
using OrderService.Data;
using OrderService.Messaging;
using OrderService.Services;

namespace OrderService.Consumers;

/// <summary>
/// The OrderService half of the choreography saga. It listens for the inventory outcome and
/// advances the order:
///   InventoryReserved -> order becomes Confirmed  -> publish OrderConfirmed
///   InventoryRejected -> order becomes Cancelled  -> publish OrderCancelled  (the compensation)
///
/// Idempotency: we only act when the order is still "Pending", so a duplicated message
/// (at-least-once delivery) is a harmless no-op.
/// </summary>
public class OrderSagaConsumer : IHostedService
{
    private readonly RabbitMqEventBus _bus;
    private readonly IServiceProvider _services;
    private readonly ILogger<OrderSagaConsumer> _logger;

    public OrderSagaConsumer(RabbitMqEventBus bus, IServiceProvider services, ILogger<OrderSagaConsumer> logger)
    {
        _bus = bus;
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _bus.StartConsuming(HandleAsync);
        return Task.CompletedTask;
    }

    private async Task HandleAsync(string eventType, string json, string correlationId)
    {
        if (eventType != "InventoryReserved" && eventType != "InventoryRejected")
            return; // not for us

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        if (eventType == "InventoryReserved")
        {
            var msg = JsonSerializer.Deserialize<InventoryReservedMessage>(json)!;
            var order = await db.Orders.FindAsync(msg.OrderId);
            if (order is null || order.Status != "Pending") return; // idempotent guard

            order.Status = SagaDecision.FromReservation(true);   // -> "Confirmed"
            await db.SaveChangesAsync();
            _bus.Publish("OrderConfirmed",
                new OrderConfirmedMessage(order.Id, order.UserId, $"Your order #{order.Id} was confirmed."),
                correlationId);
            _logger.LogInformation("[{CorrelationId}] Order {OrderId} CONFIRMED", correlationId, order.Id);
        }
        else // InventoryRejected -> compensate by cancelling the order
        {
            var msg = JsonSerializer.Deserialize<InventoryRejectedMessage>(json)!;
            var order = await db.Orders.FindAsync(msg.OrderId);
            if (order is null || order.Status != "Pending") return; // idempotent guard

            order.Status = SagaDecision.FromReservation(false);  // -> "Cancelled"
            await db.SaveChangesAsync();
            _bus.Publish("OrderCancelled",
                new OrderCancelledMessage(order.Id, order.UserId, $"Your order #{order.Id} was rejected: {msg.Reason}"),
                correlationId);
            _logger.LogWarning("[{CorrelationId}] Order {OrderId} CANCELLED (compensation): {Reason}",
                correlationId, order.Id, msg.Reason);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
