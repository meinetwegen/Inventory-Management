using Bunit;
using InventoryManagement.Web.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace InventoryManagement.Tests;

public class PaginationTests : TestContext
{
    [Fact]
    public void NextButton_RaisesPageChanged()
    {
        var page = 0;
        var cut = RenderComponent<Pagination>(p => p
            .Add(c => c.Page, 1).Add(c => c.PageSize, 10).Add(c => c.TotalCount, 35)
            .Add(c => c.PageChanged, EventCallback.Factory.Create<int>(this, x => page = x)));
        cut.FindAll("button")[1].Click();
        Assert.Equal(2, page);
    }

    [Fact]
    public void SinglePage_ShowsNothing()
    {
        var cut = RenderComponent<Pagination>(p => p.Add(c => c.TotalCount, 5).Add(c => c.PageSize, 10));
        Assert.Empty(cut.FindAll("button"));
    }
}
