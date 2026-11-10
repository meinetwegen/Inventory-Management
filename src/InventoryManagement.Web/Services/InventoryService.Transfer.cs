using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public partial class InventoryService
{
    public async Task<Result<string>> TransferAsync(SessionUser actor, string? barcode, string? fromCode, string? toCode, int quantity)
    {
        await using var db = await factory.CreateDbContextAsync();
        var product = await FindProductAsync(db, barcode);
        if (product is null) return Result.Fail<string>("Unknown barcode");

        var from = await db.Bins.FirstOrDefaultAsync(b => b.Code == fromCode);
        var to = await db.Bins.FirstOrDefaultAsync(b => b.Code == toCode);
        if (from is null || to is null) return Result.Fail<string>("Choose source and target bins");

        var source = await db.StockItems.FirstOrDefaultAsync(s => s.ProductId == product.Id && s.BinId == from.Id);
        var target = await db.StockItems.FirstOrDefaultAsync(s => s.ProductId == product.Id && s.BinId == to.Id);
        var targetUsed = await db.StockItems.Where(s => s.BinId == to.Id).SumAsync(s => (int?)s.Quantity) ?? 0;

        var check = TransferLogic.Validate(from.Code, to.Code, quantity, source?.Quantity ?? 0, to.Capacity - targetUsed);
        if (!check.IsOk) return Result.Fail<string>(check.Message);

        source!.Quantity -= quantity; source.Version++;
        if (target is null)
            db.StockItems.Add(new StockItem { ProductId = product.Id, BinId = to.Id, Quantity = quantity });
        else { target.Quantity += quantity; target.Version++; }

        db.Transactions.Add(new StockTransaction
        {
            Type = TxType.Transfer, ProductId = product.Id, FromBinId = from.Id, ToBinId = to.Id,
            Quantity = quantity, UserId = actor.Id
        });
        return await SaveAsync(db, actor, "transfer", $"Moved {quantity} pcs of {product.Name} from {from.Code} to {to.Code}");
    }
}
