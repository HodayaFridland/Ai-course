namespace NotificationService.DTOs;

/// <summary>
/// What a caller sends to record/send a notification.
/// In Phase 2 the OrderService calls this over HTTP; in Phase 4 it arrives as an event from the broker.
/// </summary>
public record CreateNotificationDto(int OrderId, int UserId, string Status, string Message);
