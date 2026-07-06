namespace OrderService.Messaging;

// The message payloads that travel over RabbitMQ during the order saga.
// Property names must match across services (they are (de)serialized as JSON).

public record SagaItem(string ProductId, int Quantity);

// OrderService --> everyone: "a new order was placed, here are its items"
public record OrderPlacedMessage(int OrderId, int UserId, List<SagaItem> Items);

// InventoryService --> OrderService: the reservation outcome
public record InventoryReservedMessage(int OrderId);
public record InventoryRejectedMessage(int OrderId, string Reason);

// OrderService --> NotificationService: the final state of the order
public record OrderConfirmedMessage(int OrderId, int UserId, string Message);
public record OrderCancelledMessage(int OrderId, int UserId, string Message);
