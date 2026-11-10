namespace InventoryManagement.Web.Logic;

public record BinStock(int BinId, string Code, int Quantity);
public record Pick(int BinId, string Code, int Quantity);

public static class PickPlanner
{
    // Take from the smallest stocks first so bins get emptied.
    public static Result<List<Pick>> Plan(IEnumerable<BinStock> bins, int quantity)
    {
        if (quantity <= 0) return Result.Fail<List<Pick>>("Quantity must be greater than 0");
        var list = bins.Where(b => b.Quantity > 0).OrderBy(b => b.Quantity).ToList();
        var available = list.Sum(b => b.Quantity);
        if (available < quantity)
            return Result.Fail<List<Pick>>($"Not enough stock: have {available}, need {quantity}");

        var picks = new List<Pick>();
        var left = quantity;
        foreach (var b in list)
        {
            if (left == 0) break;
            var take = Math.Min(left, b.Quantity);
            picks.Add(new Pick(b.BinId, b.Code, take));
            left -= take;
        }
        return Result.Ok(picks);
    }
}
