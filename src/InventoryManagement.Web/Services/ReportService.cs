using InventoryManagement.Web.Data;
using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public class ReportService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<Result<List<ProductStock>>> LowStockAsync(SessionUser actor)
    {
        if (!Permissions.CanSeeReports(actor.Role)) return Result.Fail<List<ProductStock>>("No access to reports");
        await using var db = await factory.CreateDbContextAsync();
        var stock = await InventoryState.LoadProductStockAsync(db);
        return Result.Ok(stock.LowStock().ToList());
    }

    public async Task<Result<List<MovementRow>>> MovementsAsync(SessionUser actor, int days = 30)
    {
        if (!Permissions.CanSeeReports(actor.Role)) return Result.Fail<List<MovementRow>>("No access to reports");
        await using var db = await factory.CreateDbContextAsync();
        var since = DateTime.UtcNow.AddDays(-days);
        var txs = await db.Transactions.AsNoTracking().Where(t => t.CreatedAt >= since).ToListAsync();
        return Result.Ok(txs.Movements().ToList());
    }
}
