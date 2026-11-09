using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Bin> Bins => Set<Bin>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockTransaction> Transactions => Set<StockTransaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AppUser>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Sku).IsUnique();
        b.Entity<Product>().HasIndex(p => p.Barcode).IsUnique();
        b.Entity<Bin>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Bin>().ToTable(t => t.HasCheckConstraint("CK_Bin_Capacity", "Capacity > 0"));

        b.Entity<StockItem>().HasIndex(s => new { s.ProductId, s.BinId }).IsUnique();
        b.Entity<StockItem>().Property(s => s.Version).IsConcurrencyToken();
        b.Entity<StockItem>().ToTable(t => t.HasCheckConstraint("CK_Stock_Qty", "Quantity >= 0"));

        b.Entity<StockTransaction>().ToTable(t => t.HasCheckConstraint("CK_Tx_Qty", "Quantity > 0"));
        b.Entity<StockTransaction>().HasIndex(t => t.CreatedAt);
        b.Entity<StockTransaction>().HasOne(t => t.FromBin).WithMany()
            .HasForeignKey(t => t.FromBinId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockTransaction>().HasOne(t => t.ToBin).WithMany()
            .HasForeignKey(t => t.ToBinId).OnDelete(DeleteBehavior.Restrict);
    }
}
