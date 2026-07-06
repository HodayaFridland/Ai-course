using InventoryService.Models;

namespace InventoryService.Data;

/// <summary>
/// Seeds starting stock. Note "mouse" starts at 0 on purpose — that is our
/// out-of-stock product for demonstrating the saga COMPENSATION path later.
/// </summary>
public static class InventorySeeder
{
    public static void Seed(InventoryDbContext db, ILogger logger)
    {
        if (db.InventoryItems.Any())
        {
            logger.LogInformation("Inventory already seeded — skipping.");
            return;
        }

        db.InventoryItems.AddRange(
            new InventoryItem { ProductId = "tshirt", AvailableStock = 100 },
            new InventoryItem { ProductId = "book",   AvailableStock = 50 },
            new InventoryItem { ProductId = "mouse",  AvailableStock = 0 }   // out of stock on purpose
        );
        db.SaveChanges();
        logger.LogInformation("Seeded inventory: tshirt=100, book=50, mouse=0.");
    }
}
