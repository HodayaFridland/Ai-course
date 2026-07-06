namespace NotificationService.Messaging;

// The final-state messages this service reacts to (published by OrderService).
public record OrderConfirmedMessage(int OrderId, int UserId, string Message);
public record OrderCancelledMessage(int OrderId, int UserId, string Message);
