using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;

namespace InventoryManagement.Tests;

public class CsvExportTests
{
    [Fact] public void Escape_WrapsCommasAndDoublesQuotes()
    {
        Assert.Equal("\"a,b\"", CsvExport.Escape("a,b"));
        Assert.Equal("\"say \"\"hi\"\"\"", CsvExport.Escape("say \"hi\""));
    }

    [Fact] public void Escape_BlocksFormulas() =>
        Assert.Equal("'=SUM(A1)", CsvExport.Escape("=SUM(A1)"));

    [Fact] public void Escape_NullBecomesEmpty() =>
        Assert.Equal("", CsvExport.Escape(null));

    [Fact] public void LowStock_HasHeaderAndRows() =>
        Assert.Equal("SKU,Name,In stock,Minimum\r\nA,Mouse,0,20\r\n",
            CsvExport.LowStock(new[] { new ProductStock("A", "Mouse", 20, 0) }));

    [Fact] public void LowStock_Empty_HasOnlyHeader() =>
        Assert.Equal("SKU,Name,In stock,Minimum\r\n", CsvExport.LowStock(Array.Empty<ProductStock>()));

    [Fact] public void BinFill_CalculatesPercent() =>
        Assert.Equal("Bin,Used,Capacity,Fill %\r\nA-01,50,100,50\r\n",
            CsvExport.BinFill(new[] { new BinFillRow("A-01", 50, 100) }));

    [Fact] public void Movements_FormatsDateAndType() =>
        Assert.Equal("Day,Type,Units\r\n2026-01-05,Inbound,12\r\n",
            CsvExport.Movements(new[] { new MovementRow(new DateOnly(2026, 1, 5), TxType.Inbound, 12) }));
}
