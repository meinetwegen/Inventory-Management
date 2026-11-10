using InventoryManagement.Web.Logic;

namespace InventoryManagement.Tests;

public class PickPlannerTests
{
    [Fact]
    public void TakesFromSmallestStockFirst()
    {
        var plan = PickPlanner.Plan(new[] { new BinStock(1, "A", 50), new BinStock(2, "B", 5) }, 3).Unwrap();
        Assert.Single(plan);
        Assert.Equal(2, plan[0].BinId);
    }

    [Fact]
    public void SpansTwoBins()
    {
        var plan = PickPlanner.Plan(new[] { new BinStock(1, "A", 50), new BinStock(2, "B", 5) }, 20).Unwrap();
        Assert.Equal(new[] { 5, 15 }, plan.Select(p => p.Quantity));
    }

    [Fact]
    public void NotEnough_FailsWithNumbers() =>
        Assert.Equal("Not enough stock: have 5, need 9", PickPlanner.Plan(new[] { new BinStock(1, "A", 5) }, 9).Message);

    [Fact] public void ZeroQuantity_Fails() =>
        Assert.False(PickPlanner.Plan(new[] { new BinStock(1, "A", 5) }, 0).IsOk);
}
