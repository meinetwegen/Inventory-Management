using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public partial class InventoryService
{
    public async Task<Result<string>> WriteOffAsync(SessionUser actor, string? barcode, string? binCode, int quantity, string? reason)
    {
        if (!Permissions.CanWriteOff(actor.Role))
            return Result.Fail<string>("Only managers and administrators can write off stock");

        await using var db = await factory.CreateDbContextAsync();
        var product = await FindProductAsync(db, barcode);
        if (product is null) return Result.Fail<string>("Unknown barcode");
        var bin = await db.Bins.FirstOrDefaultAsync(b => b.Code == binCode);
        if (bin is null) return Result.Fail<string>("Choose a bin");

        var item = await db.StockItems.FirstOrDefaultAsync(s => s.ProductId == product.Id && s.BinId == bin.Id);
        var check = WriteOffLogic.Validate(reason, quantity, item?.Quantity ?? 0);
        if (!check.IsOk) return Result.Fail<string>(check.Message);

        item!.Quantity -= quantity; item.Version++;
        db.Transactions.Add(new StockTransaction
        {
            Type = TxType.WriteOff, ProductId = product.Id, FromBinId = bin.Id,
            Quantity = quantity, Reason = reason!.Trim(), UserId = actor.Id
        });
        return await SaveAsync(db, actor, "write-off", $"Wrote off {quantity} pcs of {product.Name} from {bin.Code}");
    }
}
