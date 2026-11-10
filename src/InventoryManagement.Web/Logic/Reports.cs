using InventoryManagement.Web.Models;

namespace InventoryManagement.Web.Logic;

public record ProductStock(string Sku, string Name, int Min, int Qty);
public record MovementRow(DateOnly Day, TxType Type, int Total);

public static class Reports
{
    public static IEnumerable<ProductStock> LowStock(this IEnumerable<ProductStock> items) =>
        items.Where(x => x.Qty < x.Min).OrderBy(x => x.Qty - x.Min);

    public static IEnumerable<MovementRow> Movements(this IEnumerable<StockTransaction> txs) =>
        txs.GroupBy(t => (Day: DateOnly.FromDateTime(t.CreatedAt), t.Type))
           .Select(g => new MovementRow(g.Key.Day, g.Key.Type, g.Sum(t => t.Quantity)))
           .OrderByDescending(r => r.Day).ThenBy(r => r.Type);

    public static int FillPercent(int occupied, int capacity) =>
        capacity <= 0 ? 0 : occupied * 100 / capacity;
}
