using InventoryManagement.Web.Data;
using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public class ProductService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<(List<Product> Items, int Total)> ListAsync(string? search, int page, int pageSize)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.Products.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) || p.Sku.Contains(search) || p.Barcode.Contains(search));

        var total = await q.CountAsync();
        var items = await q.OrderBy(p => p.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Result<Product>> SaveAsync(SessionUser actor, int? id, string? sku, string? barcode, string? name, int minStock)
    {
        if (!Permissions.CanManageCatalog(actor.Role))
            return Result.Fail<Product>("Only administrators can edit products");

        var valid = ProductLogic.Validate(sku, barcode, name, minStock);
        if (!valid.IsOk) return Result.Fail<Product>(valid.Message);
        var v = valid.Unwrap();

        await using var db = await factory.CreateDbContextAsync();
        var current = id ?? 0;
        if (await db.Products.AnyAsync(p => (p.Sku == v.Sku || p.Barcode == v.Barcode) && p.Id != current))
            return Result.Fail<Product>("A product with this SKU or barcode already exists");

        var product = id is null ? new Product() : await db.Products.FindAsync(id.Value);
        if (product is null) return Result.Fail<Product>("Product not found");
        product.Sku = v.Sku; product.Barcode = v.Barcode; product.Name = v.Name; product.MinStockLevel = v.MinStock;
        if (id is null) db.Products.Add(product);
        await db.SaveChangesAsync();
        return Result.Ok(product);
    }

    // products are never deleted, only disabled
    public async Task<Result<bool>> DisableAsync(SessionUser actor, int id)
    {
        if (!Permissions.CanManageCatalog(actor.Role))
            return Result.Fail<bool>("Only administrators can disable products");
        await using var db = await factory.CreateDbContextAsync();
        var p = await db.Products.FindAsync(id);
        if (p is null) return Result.Fail<bool>("Product not found");
        p.IsActive = false;
        await db.SaveChangesAsync();
        return Result.Ok(true);
    }
}
