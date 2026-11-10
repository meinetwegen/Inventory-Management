using InventoryManagement.Web.Data;
using InventoryManagement.Web.Logic;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public record Summary(int Products, int Units, int BinsUsed, int BinCount, int LowStockCount);

// State container for the dashboard; refreshed after every stock operation.
public class InventoryState(IDbContextFactory<AppDbContext> factory)
{
    public Summary? Current { get; private set; }
    public List<ProductStock> LowStock { get; private set; } = new();
    public event Action? OnChange;

    public async Task RefreshAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var stock = await LoadProductStockAsync(db);
        var low = stock.LowStock().ToList();
        var units = stock.Sum(s => s.Qty);
        var binCount = await db.Bins.CountAsync();
        var binsUsed = await db.StockItems.Where(s => s.Quantity > 0).Select(s => s.BinId).Distinct().CountAsync();

        LowStock = low.Take(5).ToList();
        Current = new Summary(stock.Count, units, binsUsed, binCount, low.Count);
        OnChange?.Invoke();
    }

    public static async Task<List<ProductStock>> LoadProductStockAsync(AppDbContext db)
    {
        var totals = await db.StockItems.GroupBy(s => s.ProductId)
            .Select(g => new { Id = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.Id, x => x.Qty);
        var products = await db.Products.AsNoTracking().Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Sku, p.Name, p.MinStockLevel }).ToListAsync();
        return products.Select(p => new ProductStock(p.Sku, p.Name, p.MinStockLevel, totals.GetValueOrDefault(p.Id))).ToList();
    }
}
