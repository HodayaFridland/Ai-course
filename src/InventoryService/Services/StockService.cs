using InventoryService.Data;
using InventoryService.DTOs;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Services;

public interface IStockService
{
    Task<List<InventoryItem>> GetAllAsync();
    Task<InventoryItem?> GetAsync(string productId);
    Task<InventoryItem> SetStockAsync(SetStockDto dto);
    Task<ReserveResult> ReserveAsync(ReserveRequest request);
    Task ReleaseAsync(ReserveRequest request);
}

public class StockService : IStockService
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<StockService> _logger;

    public StockService(InventoryDbContext db, ILogger<StockService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<InventoryItem>> GetAllAsync() =>
        await _db.InventoryItems.ToListAsync();

    public async Task<InventoryItem?> GetAsync(string productId) =>
        await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId);

    // Create the stock row if it doesn't exist yet, otherwise overwrite the available quantity.
    public async Task<InventoryItem> SetStockAsync(SetStockDto dto)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == dto.ProductId);
        if (item == null)
        {
            item = new InventoryItem { ProductId = dto.ProductId, AvailableStock = dto.Quantity };
            _db.InventoryItems.Add(item);
        }
        else
        {
            item.AvailableStock = dto.Quantity;
        }
        await _db.SaveChangesAsync();
        return item;
    }

    /// <summary>
    /// Reserve stock for an order — ALL items or NONE (this is the ACID "atomicity" the assignment asks about).
    /// We open one database transaction and try each item with a GUARDED update:
    /// the UPDATE only touches the row if AvailableStock is still high enough. If any item can't be
    /// satisfied we roll the whole transaction back, so no partial reservation is ever left behind.
    /// </summary>
    public async Task<ReserveResult> ReserveAsync(ReserveRequest request)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var line in request.Items)
        {
            // A single atomic SQL UPDATE with a WHERE guard. Returns the number of rows changed.
            // If AvailableStock < Quantity, zero rows change → we cannot fulfil this line.
            var affected = await _db.InventoryItems
                .Where(i => i.ProductId == line.ProductId && i.AvailableStock >= line.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.AvailableStock, i => i.AvailableStock - line.Quantity)
                    .SetProperty(i => i.ReservedStock, i => i.ReservedStock + line.Quantity));

            if (affected == 0)
            {
                await transaction.RollbackAsync();
                var reason = $"Insufficient stock for product '{line.ProductId}'.";
                _logger.LogWarning("Order {OrderId} reservation REJECTED: {Reason}", request.OrderId, reason);
                return new ReserveResult(false, reason);
            }
        }

        await transaction.CommitAsync();
        _logger.LogInformation("Order {OrderId} reservation CONFIRMED for {Count} item(s).",
            request.OrderId, request.Items.Count);
        return new ReserveResult(true, null);
    }

    /// <summary>
    /// Compensation: give the stock back (used when an order is cancelled after a reservation).
    /// Moves quantity from ReservedStock back to AvailableStock.
    /// </summary>
    public async Task ReleaseAsync(ReserveRequest request)
    {
        foreach (var line in request.Items)
        {
            // Only release what was actually reserved (guard prevents ReservedStock going negative).
            await _db.InventoryItems
                .Where(i => i.ProductId == line.ProductId && i.ReservedStock >= line.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.AvailableStock, i => i.AvailableStock + line.Quantity)
                    .SetProperty(i => i.ReservedStock, i => i.ReservedStock - line.Quantity));
        }
        _logger.LogInformation("Order {OrderId} reservation RELEASED (compensation).", request.OrderId);
    }
}
