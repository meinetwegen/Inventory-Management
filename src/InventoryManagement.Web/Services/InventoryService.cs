using InventoryManagement.Web.Data;
using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public partial class InventoryService(
    IDbContextFactory<AppDbContext> factory, InventoryState state, ILogger<InventoryService> log)
{
    const string ConflictMessage = "Stock was changed by someone else. Please refresh and try again.";

    static Task<Product?> FindProductAsync(AppDbContext db, string? barcode)
    {
        var code = barcode?.Trim() ?? "";
        return db.Products.FirstOrDefaultAsync(p => p.Barcode == code && p.IsActive);
    }

    async Task<Result<string>> SaveAsync(AppDbContext db, SessionUser actor, string operation, string message)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            log.LogWarning("Concurrency conflict in {Op} by {User}", operation, actor.Email);
            return Result.Fail<string>(ConflictMessage);
        }
        catch (DbUpdateException ex)
        {
            log.LogError(ex, "Database error in {Op}", operation);
            return Result.Fail<string>("Could not save changes. Check the values and try again.");
        }
        log.LogInformation("{User} {Op}: {Message}", actor.Email, operation, message);
        await state.RefreshAsync();
        return Result.Ok(message);
    }
}
