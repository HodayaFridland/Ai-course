namespace OrderService.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }

    // The product's id in the catalog (e.g. "tshirt"). We store the id, not a foreign key.
    public string ProductId { get; set; } = string.Empty;

    // Snapshot of the name and price AT THE TIME OF ORDER — so a later price change in the
    // catalog does not rewrite history. This is why the order can be self-contained.
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }

    public Order? Order { get; set; }
}
