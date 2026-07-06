using Microsoft.EntityFrameworkCore;
using InventoryService.Models;

namespace InventoryService.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One stock row per product — enforce uniqueness on ProductId.
        modelBuilder.Entity<InventoryItem>()
            .HasIndex(i => i.ProductId)
            .IsUnique();
    }
}
