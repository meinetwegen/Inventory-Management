using InventoryManagement.Web.Models;

namespace InventoryManagement.Web.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db, bool demo)
    {
        if (db.Users.Any()) return;

        db.Users.AddRange(
            new AppUser { Email = "admin@demo.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), Role = Role.Administrator },
            new AppUser { Email = "manager@demo.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"), Role = Role.Manager },
            new AppUser { Email = "operator@demo.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Operator123!"), Role = Role.Operator });

        db.Bins.AddRange(
            new Bin { Code = "A-01", Capacity = 100 }, new Bin { Code = "A-02", Capacity = 100 },
            new Bin { Code = "B-01", Capacity = 50 }, new Bin { Code = "B-02", Capacity = 50 },
            new Bin { Code = "C-01", Capacity = 20 });

        db.Products.AddRange(
            new Product { Sku = "LAP-001", Barcode = "4006381333931", Name = "Laptop 15\"", MinStockLevel = 5 },
            new Product { Sku = "MOU-001", Barcode = "5901234123457", Name = "Wireless Mouse", MinStockLevel = 20 },
            new Product { Sku = "KEY-001", Barcode = "9780201379624", Name = "Mechanical Keyboard", MinStockLevel = 10 },
            new Product { Sku = "MON-001", Barcode = "7312345678901", Name = "Monitor 27\"", MinStockLevel = 4 });

        if (demo)
            for (var i = 1; i <= 200; i++)
                db.Products.Add(new Product { Sku = $"DEMO-{i:D3}", Barcode = $"2000000{i:D6}", Name = $"Demo item {i}", MinStockLevel = i % 10 });

        db.SaveChanges();
    }
}
