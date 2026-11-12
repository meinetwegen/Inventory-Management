using InventoryManagement.Web.Data;
using InventoryManagement.Web.Models;
using InventoryManagement.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace InventoryManagement.Tests;

class TestFactory : IDbContextFactory<AppDbContext>, IDisposable
{
    readonly SqliteConnection _conn = new("DataSource=:memory:");

    public TestFactory()
    {
        _conn.Open();
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        db.Users.Add(new AppUser { Email = "t@t.com", PasswordHash = "x", Role = Role.Administrator });
        db.Products.Add(new Product { Sku = "P1", Barcode = "12345678", Name = "Test item" });
        db.Bins.AddRange(new Bin { Code = "A", Capacity = 100 }, new Bin { Code = "B", Capacity = 20 });
        db.SaveChanges();
    }

    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options);

    public void Dispose() => _conn.Dispose();
}

public class InventoryServiceTests
{
    static readonly SessionUser Admin = new(1, "t@t.com", Role.Administrator);
    static readonly SessionUser Op = new(1, "t@t.com", Role.Operator);

    static (InventoryService Svc, TestFactory F) Create()
    {
        var f = new TestFactory();
        return (new InventoryService(f, new InventoryState(f), NullLogger<InventoryService>.Instance), f);
    }

    [Fact]
    public async Task Receive_CreatesStockAndTransaction()
    {
        var (svc, f) = Create();
        var r = await svc.ReceiveAsync(Admin, "12345678", 10);
        Assert.True(r.IsOk);
        using var db = f.CreateDbContext();
        Assert.Equal(10, db.StockItems.Sum(s => s.Quantity));
        Assert.Equal(TxType.Inbound, Assert.Single(db.Transactions).Type);
    }

    [Fact]
    public async Task Receive_TwiceGoesToSameBin()
    {
        var (svc, f) = Create();
        await svc.ReceiveAsync(Admin, "12345678", 10);
        await svc.ReceiveAsync(Admin, "12345678", 5);
        using var db = f.CreateDbContext();
        var item = Assert.Single(db.StockItems);
        Assert.Equal(15, item.Quantity);
    }

    [Fact]
    public async Task Receive_NoSpace_ChangesNothing()
    {
        var (svc, f) = Create();
        var r = await svc.ReceiveAsync(Admin, "12345678", 500);
        Assert.False(r.IsOk);
        using var db = f.CreateDbContext();
        Assert.Empty(db.StockItems);
        Assert.Empty(db.Transactions);
    }

    [Fact]
    public async Task Receive_UnknownBarcode_Fails()
    {
        var (svc, _) = Create();
        Assert.False((await svc.ReceiveAsync(Admin, "00000000", 1)).IsOk);
    }

    [Fact]
    public async Task Ship_NotEnough_ChangesNothing()
    {
        var (svc, f) = Create();
        await svc.ReceiveAsync(Admin, "12345678", 5);
        var r = await svc.ShipAsync(Admin, "12345678", 9);
        Assert.False(r.IsOk);
        using var db = f.CreateDbContext();
        Assert.Equal(5, db.StockItems.Sum(s => s.Quantity));
        Assert.Single(db.Transactions);
    }

    [Fact]
    public async Task Transfer_OverCapacity_Fails()
    {
        var (svc, _) = Create();
        await svc.ReceiveAsync(Admin, "12345678", 50);
        var r = await svc.TransferAsync(Admin, "12345678", "A", "B", 30);
        Assert.False(r.IsOk);
    }

    [Fact]
    public async Task WriteOff_OperatorIsRejected()
    {
        var (svc, _) = Create();
        await svc.ReceiveAsync(Admin, "12345678", 5);
        Assert.False((await svc.WriteOffAsync(Op, "12345678", "B", 1, "damaged")).IsOk);
    }

    [Fact]
    public async Task WriteOff_WithoutReason_Fails()
    {
        var (svc, _) = Create();
        await svc.ReceiveAsync(Admin, "12345678", 5);
        Assert.False((await svc.WriteOffAsync(Admin, "12345678", "B", 1, "")).IsOk);
    }

    [Fact]
    public async Task FullScenario_Receive_Transfer_Ship_History()
    {
        var (svc, f) = Create();
        Assert.True((await svc.ReceiveAsync(Admin, "12345678", 20)).IsOk);
        Assert.True((await svc.TransferAsync(Admin, "12345678", "B", "A", 10)).IsOk);
        Assert.True((await svc.ShipAsync(Admin, "12345678", 15)).IsOk);

        var scan = (await svc.ScanAsync("12345678")).Unwrap();
        Assert.Equal(5, scan.Total);

        var (history, total) = await svc.ListTransactionsAsync(null, 1, 10);
        Assert.Equal(4, total);
        Assert.Contains(history, h => h.Type == TxType.Outbound);
    }

    [Fact]
    public async Task ConcurrentChange_IsReportedAsFail()
    {
        var (svc, f) = Create();
        await svc.ReceiveAsync(Admin, "12345678", 10);
        using var db1 = f.CreateDbContext();
        using var db2 = f.CreateDbContext();
        var a = db1.StockItems.First(); var b = db2.StockItems.First();
        a.Quantity += 1; a.Version++; db1.SaveChanges();
        b.Quantity += 1; b.Version++;
        Assert.Throws<DbUpdateConcurrencyException>(() => db2.SaveChanges());
    }
}
