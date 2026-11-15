using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;

namespace InventoryManagement.Tests;

// Pure business rules: no database and no UI are needed to test them.
public class RulesTests
{
    // ---------- TransferLogic ----------

    [Fact]
    public void Transfer_ValidRequest_ReturnsQuantity()
    {
        var r = TransferLogic.Validate("A", "B", 5, availableInSource: 10, freeInTarget: 20);
        Assert.True(r.IsOk);
        Assert.Equal(5, r.Unwrap());
    }

    [Fact]
    public void Transfer_ZeroQuantity_Fails()
    {
        var r = TransferLogic.Validate("A", "B", 0, 10, 20);
        Assert.False(r.IsOk);
        Assert.Contains("greater than 0", r.Message);
    }

    [Fact]
    public void Transfer_SameBin_Fails()
    {
        var r = TransferLogic.Validate("A", "A", 1, 10, 20);
        Assert.False(r.IsOk);
        Assert.Contains("different", r.Message);
    }

    [Fact]
    public void Transfer_NotEnoughInSource_FailsAndNamesTheNumbers()
    {
        var r = TransferLogic.Validate("A", "B", 8, availableInSource: 3, freeInTarget: 20);
        Assert.False(r.IsOk);
        Assert.Contains("have 3", r.Message);
        Assert.Contains("need 8", r.Message);
    }

    [Fact]
    public void Transfer_TargetTooFull_Fails()
    {
        var r = TransferLogic.Validate("A", "B", 8, availableInSource: 10, freeInTarget: 2);
        Assert.False(r.IsOk);
        Assert.Contains("2 free places", r.Message);
    }

    // ---------- WriteOffLogic ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WriteOff_WithoutReason_Fails(string? reason)
    {
        var r = WriteOffLogic.Validate(reason, 1, 10);
        Assert.False(r.IsOk);
        Assert.Contains("Reason", r.Message);
    }

    [Fact]
    public void WriteOff_ZeroQuantity_Fails()
    {
        Assert.False(WriteOffLogic.Validate("damaged", 0, 10).IsOk);
    }

    [Fact]
    public void WriteOff_MoreThanAvailable_Fails()
    {
        var r = WriteOffLogic.Validate("damaged", 11, 10);
        Assert.False(r.IsOk);
        Assert.Contains("only 10", r.Message);
    }

    [Fact]
    public void WriteOff_ValidRequest_ReturnsQuantity()
    {
        Assert.Equal(4, WriteOffLogic.Validate("expired", 4, 10).Unwrap());
    }

    // ---------- Permissions ----------

    [Theory]
    //          role                  catalog  writeOff reports
    [InlineData(Role.Administrator,   true,    true,    true)]
    [InlineData(Role.Manager,         false,   true,    true)]
    [InlineData(Role.Operator,        false,   false,   false)]
    public void Permissions_MatchTheRoleTable(Role role, bool catalog, bool writeOff, bool reports)
    {
        Assert.Equal(catalog, Permissions.CanManageCatalog(role));
        Assert.Equal(writeOff, Permissions.CanWriteOff(role));
        Assert.Equal(reports, Permissions.CanSeeReports(role));
    }

    // ---------- Reports ----------

    [Fact]
    public void LowStock_ReturnsOnlyBelowMinimum_LargestShortageFirst()
    {
        var items = new[]
        {
            new ProductStock("B", "b", Min: 10, Qty: 9),    // short by 1
            new ProductStock("A", "a", Min: 10, Qty: 3),    // short by 7
            new ProductStock("C", "c", Min: 5, Qty: 5),     // exactly at minimum: not low
            new ProductStock("D", "d", Min: 5, Qty: 50)
        };

        var result = items.LowStock().Select(x => x.Sku).ToList();

        Assert.Equal(new[] { "A", "B" }, result);
    }

    [Fact]
    public void Movements_GroupByDayAndType_NewestDayFirst()
    {
        var jan1 = new DateTime(2026, 1, 1, 10, 0, 0);
        var jan2 = new DateTime(2026, 1, 2, 9, 0, 0);
        var txs = new[]
        {
            new StockTransaction { Type = TxType.Inbound, Quantity = 5, CreatedAt = jan1 },
            new StockTransaction { Type = TxType.Inbound, Quantity = 3, CreatedAt = jan1.AddHours(2) },
            new StockTransaction { Type = TxType.Outbound, Quantity = 2, CreatedAt = jan1 },
            new StockTransaction { Type = TxType.Inbound, Quantity = 1, CreatedAt = jan2 }
        };

        var rows = txs.Movements().ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(new MovementRow(new DateOnly(2026, 1, 2), TxType.Inbound, 1), rows[0]);
        Assert.Equal(new MovementRow(new DateOnly(2026, 1, 1), TxType.Inbound, 8), rows[1]);
        Assert.Equal(new MovementRow(new DateOnly(2026, 1, 1), TxType.Outbound, 2), rows[2]);
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(50, 100, 50)]
    [InlineData(1, 3, 33)]      // whole percent, rounded down
    [InlineData(5, 0, 0)]       // no capacity: no division by zero
    public void FillPercent_IsWholePercentAndNeverDividesByZero(int occupied, int capacity, int expected)
    {
        Assert.Equal(expected, Reports.FillPercent(occupied, capacity));
    }
}
