namespace InventoryManagement.Web.Logic;

public static class WriteOffLogic
{
    public static Result<int> Validate(string? reason, int quantity, int available)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result.Fail<int>("Reason is required");
        if (quantity <= 0) return Result.Fail<int>("Quantity must be greater than 0");
        if (quantity > available) return Result.Fail<int>($"Cannot write off {quantity}: only {available} in the bin");
        return Result.Ok(quantity);
    }
}
