using System.Text.Json;
using NotificationService.Messaging;
using NotificationService.Models;
using NotificationService.Repositories;

namespace NotificationService.Consumers;

/// <summary>
/// The NotificationService half of the saga: the last step. It listens for the order's final
/// state and records a notification for the customer:
///   OrderConfirmed -> "Confirmed" notification
///   OrderCancelled -> "Rejected"  notification
///
/// Idempotency: if a notification for this (order, status) already exists we skip it, so a
/// duplicated message doesn't create a second identical notification.
/// </summary>
public class NotificationSagaConsumer : IHostedService
{
    private readonly RabbitMqEventBus _bus;
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationSagaConsumer> _logger;

    public NotificationSagaConsumer(RabbitMqEventBus bus, IServiceProvider services, ILogger<NotificationSagaConsumer> logger)
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
        int orderId, userId;
        string status, message;

        switch (eventType)
        {
            case "OrderConfirmed":
                var confirmed = JsonSerializer.Deserialize<OrderConfirmedMessage>(json)!;
                (orderId, userId, status, message) = (confirmed.OrderId, confirmed.UserId, "Confirmed", confirmed.Message);
                break;
            case "OrderCancelled":
                var cancelled = JsonSerializer.Deserialize<OrderCancelledMessage>(json)!;
                (orderId, userId, status, message) = (cancelled.OrderId, cancelled.UserId, "Rejected", cancelled.Message);
                break;
            default:
                return; // not for us
        }

        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();

        // Idempotency guard: skip if we already recorded this exact notification.
        var existing = await repo.GetByUserAsync(userId);
        if (existing.Any(n => n.OrderId == orderId && n.Status == status))
        {
            _logger.LogInformation("[{CorrelationId}] Duplicate notification for order {OrderId} — ignored",
                correlationId, orderId);
            return;
        }

        await repo.CreateAsync(new Notification
        {
            OrderId = orderId,
            UserId = userId,
            Status = status,
            Message = message
        });

        _logger.LogInformation("[{CorrelationId}] NOTIFY user {UserId} about order {OrderId}: [{Status}] {Message}",
            correlationId, userId, orderId, status, message);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
