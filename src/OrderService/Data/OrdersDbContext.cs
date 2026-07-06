using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store money with a fixed precision (18 digits, 2 after the decimal point).
        modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<OrderItem>().Property(i => i.UnitPrice).HasColumnType("decimal(18,2)");
        modelBuilder.Entity<OrderItem>().Property(i => i.Subtotal).HasColumnType("decimal(18,2)");
    }
}
