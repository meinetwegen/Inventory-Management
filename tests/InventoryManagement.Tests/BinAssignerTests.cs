using InventoryManagement.Web.Logic;

namespace InventoryManagement.Tests;

public class BinAssignerTests
{
    static BinState Bin(int id, int cap, int used, bool holds = false) => new(id, $"B{id}", cap, used, holds);

    [Fact] public void PrefersBinWithSameProduct() =>
        Assert.Equal(2, BinAssigner.Choose(new[] { Bin(1, 50, 0), Bin(2, 100, 30, true) }, 10).Unwrap().BinId);

    [Fact] public void SameProduct_ChoosesFullest() =>
        Assert.Equal(3, BinAssigner.Choose(new[] { Bin(2, 100, 10, true), Bin(3, 100, 60, true) }, 10).Unwrap().BinId);

    [Fact] public void SameProductFull_FallsBackToEmpty() =>
        Assert.Equal(1, BinAssigner.Choose(new[] { Bin(1, 50, 0), Bin(2, 100, 95, true) }, 10).Unwrap().BinId);

    [Fact] public void EmptyBin_ChoosesSmallestThatFits() =>
        Assert.Equal(2, BinAssigner.Choose(new[] { Bin(1, 100, 0), Bin(2, 30, 0), Bin(3, 10, 0) }, 20).Unwrap().BinId);

    [Fact] public void ExactFit_IsAllowed() =>
        Assert.True(BinAssigner.Choose(new[] { Bin(1, 20, 0) }, 20).IsOk);

    [Fact] public void NoSpace_Fails() =>
        Assert.Equal("No free space in any bin", BinAssigner.Choose(new[] { Bin(1, 10, 5) }, 20).Message);

    [Fact] public void NonEmptyBinWithOtherProduct_IsNotUsed() =>
        Assert.False(BinAssigner.Choose(new[] { Bin(1, 100, 10, false) }, 5).IsOk);

    [Theory] [InlineData(0)] [InlineData(-5)]
    public void BadQuantity_Fails(int q) =>
        Assert.False(BinAssigner.Choose(new[] { Bin(1, 100, 0) }, q).IsOk);
}
