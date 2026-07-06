namespace OrderService.Models;

/// <summary>
/// An order. Lives in a relational DB because money needs ACID: the order and all its
/// line items are written together in one transaction, or not at all.
///
/// Notice there are NO navigation properties to User or Product — those live in OTHER
/// services now. We keep only their ids (and a price snapshot), which is how services stay decoupled.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }

    // Pending → Confirmed (stock reserved) or Cancelled (stock rejected).
    public string Status { get; set; } = "Pending";
    public string ShippingAddress { get; set; } = string.Empty;

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public List<OrderItem> OrderItems { get; set; } = new();
}
