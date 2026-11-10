namespace InventoryManagement.Web.Logic;

public record BinState(int BinId, string Code, int Capacity, int Occupied, bool HoldsProduct)
{
    public int Free => Capacity - Occupied;
}

public static class BinAssigner
{
    // 1) bin that already holds this product (the fullest one that fits)
    // 2) empty bin with the smallest capacity that fits
    // 3) otherwise: no space
    public static Result<BinState> Choose(IEnumerable<BinState> bins, int quantity)
    {
        if (quantity <= 0) return Result.Fail<BinState>("Quantity must be greater than 0");
        var list = bins.ToList();

        var same = list.Where(b => b.HoldsProduct && b.Free >= quantity)
                       .OrderByDescending(b => b.Occupied).FirstOrDefault();
        if (same is not null) return Result.Ok(same);

        var empty = list.Where(b => b.Occupied == 0 && b.Capacity >= quantity)
                        .OrderBy(b => b.Capacity).FirstOrDefault();
        return empty is not null ? Result.Ok(empty) : Result.Fail<BinState>("No free space in any bin");
    }
}
