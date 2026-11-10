using InventoryManagement.Web.Logic;

namespace InventoryManagement.Tests;

public class ProductLogicTests
{
    [Fact] public void Valid_ReturnsProduct() =>
        Assert.True(ProductLogic.Validate("A-1", "12345678", "Name", 0).IsOk);

    [Fact] public void EmptySku_Fails() =>
        Assert.False(ProductLogic.Validate(" ", "12345678", "Name", 0).IsOk);

    [Fact] public void EmptyName_Fails() =>
        Assert.False(ProductLogic.Validate("A", "12345678", "", 0).IsOk);

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789012345")]
    [InlineData("1234abcd")]
    [InlineData(null)]
    public void BadBarcode_Fails(string? barcode) =>
        Assert.Equal("Barcode must be 8-14 digits", ProductLogic.Validate("A", barcode, "N", 0).Message);

    [Fact] public void NegativeMin_Fails() =>
        Assert.False(ProductLogic.Validate("A", "12345678", "N", -1).IsOk);

    [Fact] public void Trims_Values() =>
        Assert.Equal("A", ProductLogic.Validate("  A ", "12345678", "N", 1).Unwrap().Sku);
}
