using Bunit;
using InventoryManagement.Web.Components.Shared;

namespace InventoryManagement.Tests;

public class StatCardTests : TestContext
{
    [Fact]
    public void ShowsTitleAndValue()
    {
        var cut = RenderComponent<StatCard>(p => p.Add(c => c.Title, "Products").Add(c => c.Value, "42"));
        Assert.Contains("Products", cut.Markup);
        Assert.Contains("42", cut.Markup);
    }
}
