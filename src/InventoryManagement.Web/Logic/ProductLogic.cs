namespace InventoryManagement.Web.Logic;

public record ValidProduct(string Sku, string Barcode, string Name, int MinStock);

public static class ProductLogic
{
    public static Result<ValidProduct> Validate(string? sku, string? barcode, string? name, int minStock) =>
        Required(sku, "SKU").Bind(s =>
        Required(name, "Name").Bind(n =>
        CheckBarcode(barcode).Bind(b =>
        CheckMin(minStock).Map(m => new ValidProduct(s, b, n, m)))));

    static Result<string> Required(string? v, string field) =>
        string.IsNullOrWhiteSpace(v) ? Result.Fail<string>($"{field} is required") : Result.Ok(v.Trim());

    static Result<string> CheckBarcode(string? v)
    {
        var s = v?.Trim() ?? "";
        return s.Length is >= 8 and <= 14 && s.All(char.IsDigit)
            ? Result.Ok(s)
            : Result.Fail<string>("Barcode must be 8-14 digits");
    }

    static Result<int> CheckMin(int v) =>
        v < 0 ? Result.Fail<int>("Minimum stock cannot be negative") : Result.Ok(v);
}
