using InventoryManagement.Web.Data;
using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public record BinView(int Id, string Code, int Capacity, int Occupied)
{
    public int Percent => Reports.FillPercent(Occupied, Capacity);
}

public class BinService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<BinView>> ListAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var bins = await db.Bins.AsNoTracking().OrderBy(b => b.Code).ToListAsync();
        var occupied = await db.StockItems.GroupBy(s => s.BinId)
            .Select(g => new { BinId = g.Key, Qty = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.BinId, x => x.Qty);
        return bins.Select(b => new BinView(b.Id, b.Code, b.Capacity, occupied.GetValueOrDefault(b.Id))).ToList();
    }

    public async Task<Result<Bin>> CreateAsync(SessionUser actor, string? code, int capacity)
    {
        if (!Permissions.CanManageCatalog(actor.Role)) return Result.Fail<Bin>("Only administrators can add bins");
        if (string.IsNullOrWhiteSpace(code)) return Result.Fail<Bin>("Bin code is required");
        if (capacity <= 0) return Result.Fail<Bin>("Capacity must be greater than 0");

        await using var db = await factory.CreateDbContextAsync();
        var c = code.Trim().ToUpper();
        if (await db.Bins.AnyAsync(b => b.Code == c)) return Result.Fail<Bin>("Bin code already exists");
        var bin = new Bin { Code = c, Capacity = capacity };
        db.Bins.Add(bin);
        await db.SaveChangesAsync();
        return Result.Ok(bin);
    }
}
