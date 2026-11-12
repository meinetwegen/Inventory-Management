namespace InventoryManagement.Web.Logic;

public record BinFillRow(string Code, int Occupied, int Capacity);

// Pure functions: report data in, CSV text out.
public static class CsvExport
{
    public static string Escape(string? value)
    {
        var s = value ?? "";
        // cells starting with these characters can be run as formulas by Excel
        if (s.Length > 0 && "=+-@".Contains(s[0])) s = "'" + s;
        return s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;
    }

    public static string Build(string[] header, IEnumerable<string[]> rows) =>
        string.Join("\r\n", new[] { header }.Concat(rows).Select(r => string.Join(",", r.Select(Escape)))) + "\r\n";

    public static string LowStock(IEnumerable<ProductStock> items) =>
        Build(new[] { "SKU", "Name", "In stock", "Minimum" },
            items.Select(p => new[] { p.Sku, p.Name, p.Qty.ToString(), p.Min.ToString() }));

    public static string BinFill(IEnumerable<BinFillRow> bins) =>
        Build(new[] { "Bin", "Used", "Capacity", "Fill %" },
            bins.Select(b => new[] { b.Code, b.Occupied.ToString(), b.Capacity.ToString(),
                Reports.FillPercent(b.Occupied, b.Capacity).ToString() }));

    public static string Movements(IEnumerable<MovementRow> rows) =>
        Build(new[] { "Day", "Type", "Units" },
            rows.Select(r => new[] { r.Day.ToString("yyyy-MM-dd"), r.Type.ToString(), r.Total.ToString() }));
}
