namespace InventoryService.DTOs;

// One line item to reserve/release: which product and how many.
public record ReserveItem(string ProductId, int Quantity);

// A reservation request for a whole order (all items succeed together, or none do).
public record ReserveRequest(int OrderId, List<ReserveItem> Items);

// The answer we send back.
public record ReserveResult(bool Success, string? Reason);

// Used to set/add stock for a product.
public record SetStockDto(string ProductId, int Quantity);
