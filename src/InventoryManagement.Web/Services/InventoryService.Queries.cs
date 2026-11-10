using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public record StockRow(string Sku, string Name, string Bin, int Quantity);
public record BinQty(string Bin, int Quantity);
public record ScanResult(string Sku, string Name, int Total, List<BinQty> Locations);
public record TxRow(DateTime When, TxType Type, string Sku, int Quantity, string From, string To, string User, string Reason);

public partial class InventoryService
{
    public async Task<(List<StockRow> Items, int Total)> ListStockAsync(int page, int pageSize)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.StockItems.AsNoTracking().Where(s => s.Quantity > 0);
        var total = await q.CountAsync();
        var items = await q.OrderBy(s => s.Bin.Code).ThenBy(s => s.Product.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new StockRow(s.Product.Sku, s.Product.Name, s.Bin.Code, s.Quantity))
            .ToListAsync();
        return (items, total);
    }

    public async Task<(List<TxRow> Items, int Total)> ListTransactionsAsync(TxType? type, int page, int pageSize)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.Transactions.AsNoTracking().AsQueryable();
        if (type is not null) q = q.Where(t => t.Type == type);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(t => new TxRow(t.CreatedAt, t.Type, t.Product.Sku, t.Quantity,
                t.FromBin == null ? "" : t.FromBin.Code, t.ToBin == null ? "" : t.ToBin.Code,
                t.User.Email, t.Reason ?? ""))
            .ToListAsync();
        return (items, total);
    }

    public async Task<Result<ScanResult>> ScanAsync(string? barcode)
    {
        await using var db = await factory.CreateDbContextAsync();
        var product = await FindProductAsync(db, barcode);
        if (product is null) return Result.Fail<ScanResult>("Unknown barcode");
        var locations = await db.StockItems.AsNoTracking()
            .Where(s => s.ProductId == product.Id && s.Quantity > 0)
            .OrderBy(s => s.Bin.Code)
            .Select(s => new BinQty(s.Bin.Code, s.Quantity)).ToListAsync();
        return Result.Ok(new ScanResult(product.Sku, product.Name, locations.Sum(l => l.Quantity), locations));
    }
}
