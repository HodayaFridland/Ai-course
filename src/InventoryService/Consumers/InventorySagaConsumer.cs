using System.Text.Json;
using InventoryService.DTOs;
using InventoryService.Messaging;
using InventoryService.Services;

namespace InventoryService.Consumers;

/// <summary>
/// The InventoryService half of the saga. It listens for "OrderPlaced", tries to reserve stock
/// (all-or-nothing, ACID), and answers with either "InventoryReserved" or "InventoryRejected".
///
/// Idempotency: at-least-once delivery means the same OrderPlaced could arrive twice. We remember
/// the order ids we've already handled so a duplicate does not reserve stock a second time.
/// (In-memory is enough to demonstrate the idea for this project.)
/// </summary>
public class InventorySagaConsumer : IHostedService
{
    private readonly RabbitMqEventBus _bus;
    private readonly IServiceProvider _services;
    private readonly ILogger<InventorySagaConsumer> _logger;
    private readonly HashSet<int> _processedOrders = new();
    private readonly object _lock = new();

    public InventorySagaConsumer(RabbitMqEventBus bus, IServiceProvider services, ILogger<InventorySagaConsumer> logger)
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
        if (eventType != "OrderPlaced") return; // only we reserve stock

        var msg = JsonSerializer.Deserialize<OrderPlacedMessage>(json)!;

        // Idempotency guard.
        lock (_lock)
        {
            if (!_processedOrders.Add(msg.OrderId))
            {
                _logger.LogInformation("[{CorrelationId}] Duplicate OrderPlaced for order {OrderId} — ignored",
                    correlationId, msg.OrderId);
                return;
            }
        }

        using var scope = _services.CreateScope();
        var stock = scope.ServiceProvider.GetRequiredService<IStockService>();

        var request = new ReserveRequest(msg.OrderId,
            msg.Items.Select(i => new ReserveItem(i.ProductId, i.Quantity)).ToList());
        var result = await stock.ReserveAsync(request);

        if (result.Success)
        {
            _bus.Publish("InventoryReserved", new InventoryReservedMessage(msg.OrderId), correlationId);
            _logger.LogInformation("[{CorrelationId}] Order {OrderId} stock RESERVED", correlationId, msg.OrderId);
        }
        else
        {
            _bus.Publish("InventoryRejected",
                new InventoryRejectedMessage(msg.OrderId, result.Reason ?? "Out of stock"), correlationId);
            _logger.LogWarning("[{CorrelationId}] Order {OrderId} stock REJECTED: {Reason}",
                correlationId, msg.OrderId, result.Reason);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
