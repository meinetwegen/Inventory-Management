namespace InventoryManagement.Web.Logic;

public static class TransferLogic
{
    public static Result<int> Validate(string fromCode, string toCode, int quantity, int availableInSource, int freeInTarget)
    {
        if (quantity <= 0) return Result.Fail<int>("Quantity must be greater than 0");
        if (fromCode == toCode) return Result.Fail<int>("Source and target bins must be different");
        if (availableInSource < quantity)
            return Result.Fail<int>($"Not enough stock in {fromCode}: have {availableInSource}, need {quantity}");
        if (freeInTarget < quantity)
            return Result.Fail<int>($"Bin {toCode} has only {freeInTarget} free places");
        return Result.Ok(quantity);
    }
}
