using InventoryManagement.Web.Logic;

namespace InventoryManagement.Tests;

public class ResultTests
{
    [Fact]
    public void Map_TransformsOkValue() =>
        Assert.Equal(6, Result.Ok(3).Map(x => x * 2).Unwrap());

    [Fact]
    public void Map_KeepsFail() =>
        Assert.Equal("bad", Result.Fail<int>("bad").Map(x => x * 2).Message);

    [Fact]
    public void Bind_StopsAtFirstFail()
    {
        var called = false;
        var r = Result.Fail<int>("first").Bind(x => { called = true; return Result.Ok(x); });
        Assert.False(called);
        Assert.Equal("first", r.Message);
    }

    [Fact]
    public void Bind_ChainsOk() =>
        Assert.Equal(7, Result.Ok(3).Bind(x => Result.Ok(x + 4)).Unwrap());
}
