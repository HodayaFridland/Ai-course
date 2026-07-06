namespace InventoryService.Messaging;

// Message payloads exchanged over RabbitMQ. Property names must match the other services'.

public record SagaItem(string ProductId, int Quantity);

// Received from OrderService
public record OrderPlacedMessage(int OrderId, int UserId, List<SagaItem> Items);

// Sent back to OrderService
public record InventoryReservedMessage(int OrderId);
public record InventoryRejectedMessage(int OrderId, string Reason);
