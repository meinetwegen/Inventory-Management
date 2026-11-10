using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public partial class InventoryService
{
    public async Task<Result<string>> ShipAsync(SessionUser actor, string? barcode, int quantity)
    {
        await using var db = await factory.CreateDbContextAsync();
        var product = await FindProductAsync(db, barcode);
        if (product is null) return Result.Fail<string>("Unknown barcode");

        var items = await db.StockItems.Include(s => s.Bin)
            .Where(s => s.ProductId == product.Id && s.Quantity > 0).ToListAsync();
        var plan = PickPlanner.Plan(items.Select(i => new BinStock(i.BinId, i.Bin.Code, i.Quantity)), quantity);
        if (!plan.IsOk) return Result.Fail<string>(plan.Message);

        foreach (var pick in plan.Unwrap())
        {
            var item = items.First(i => i.BinId == pick.BinId);
            item.Quantity -= pick.Quantity;
            item.Version++;
            db.Transactions.Add(new StockTransaction
            {
                Type = TxType.Outbound, ProductId = product.Id, FromBinId = pick.BinId,
                Quantity = pick.Quantity, UserId = actor.Id
            });
        }
        var from = string.Join(", ", plan.Unwrap().Select(p => $"{p.Code} ({p.Quantity})"));
        return await SaveAsync(db, actor, "ship", $"Shipped {quantity} pcs of {product.Name} from {from}");
    }
}
