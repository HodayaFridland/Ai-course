namespace OrderService.DTOs;

// ---- What the client sends to place an order ----
public record OrderItemCreateDto(string ProductId, int Quantity);
public record OrderCreateDto(int UserId, string ShippingAddress, List<OrderItemCreateDto> Items);

// ---- What we return ----
public record OrderItemResponseDto(string ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);
public record OrderResponseDto(int Id, int UserId, string Status, decimal TotalAmount, string ShippingAddress,
                              DateTime OrderDate, List<OrderItemResponseDto> Items);
