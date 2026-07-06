namespace InventoryService.Models;

/// <summary>
/// Stock record for a single product. This is the ONLY place stock lives —
/// the catalog no longer knows about stock. That is the "database per service" rule in action.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }

    // Matches the product's readable id in the catalog (e.g. "tshirt", "book").
    public string ProductId { get; set; } = string.Empty;

    // Stock that can still be sold.
    public int AvailableStock { get; set; }

    // Stock that is held for orders that are placed but not yet finalized.
    public int ReservedStock { get; set; }
}
