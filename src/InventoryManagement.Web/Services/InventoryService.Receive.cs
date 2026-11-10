using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public partial class InventoryService
{
    public async Task<Result<string>> ReceiveAsync(SessionUser actor, string? barcode, int quantity)
    {
        await using var db = await factory.CreateDbContextAsync();
        var product = await FindProductAsync(db, barcode);
        if (product is null) return Result.Fail<string>("Unknown barcode");

        var occupied = await db.StockItems.GroupBy(s => s.BinId)
            .Select(g => new { BinId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.BinId, x => x.Qty);
        var holds = await db.StockItems.Where(s => s.ProductId == product.Id && s.Quantity > 0)
            .Select(s => s.BinId).ToListAsync();
        var bins = (await db.Bins.AsNoTracking().ToListAsync())
            .Select(b => new BinState(b.Id, b.Code, b.Capacity, occupied.GetValueOrDefault(b.Id), holds.Contains(b.Id)))
            .ToList();

        var choice = BinAssigner.Choose(bins, quantity);
        if (!choice.IsOk) return Result.Fail<string>(choice.Message);
        var bin = choice.Unwrap();

        var item = await db.StockItems.FirstOrDefaultAsync(s => s.ProductId == product.Id && s.BinId == bin.BinId);
        if (item is null)
            db.StockItems.Add(new StockItem { ProductId = product.Id, BinId = bin.BinId, Quantity = quantity });
        else { item.Quantity += quantity; item.Version++; }

        db.Transactions.Add(new StockTransaction
        {
            Type = TxType.Inbound, ProductId = product.Id, ToBinId = bin.BinId, Quantity = quantity, UserId = actor.Id
        });
        return await SaveAsync(db, actor, "receive", $"Placed {quantity} pcs of {product.Name} into bin {bin.Code}");
    }
}
