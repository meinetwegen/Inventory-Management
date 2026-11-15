using Bunit;
using InventoryManagement.Web.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace InventoryManagement.Tests;

// bUnit renders the component without a browser.
public class CollapsibleSectionTests : TestContext
{
    [Fact]
    public void Collapsed_HidesContent_AndShowsPlus()
    {
        var cut = RenderComponent<CollapsibleSection>(p => p
            .Add(c => c.Title, "Stock")
            .Add(c => c.Expanded, false)
            .AddChildContent("<p id=\"inner\">Hello</p>"));

        Assert.Empty(cut.FindAll("#inner"));                       // lazy: content is not even created
        Assert.Equal("false", cut.Find("button").GetAttribute("aria-expanded"));
        Assert.Contains("+", cut.Find(".plus").TextContent);
        Assert.Contains("Stock", cut.Markup);
    }

    [Fact]
    public void Expanded_ShowsContent_AndMinus()
    {
        var cut = RenderComponent<CollapsibleSection>(p => p
            .Add(c => c.Title, "Stock")
            .Add(c => c.Expanded, true)
            .AddChildContent("<p id=\"inner\">Hello</p>"));

        Assert.Single(cut.FindAll("#inner"));
        Assert.Equal("true", cut.Find("button").GetAttribute("aria-expanded"));
        Assert.DoesNotContain("+", cut.Find(".plus").TextContent);
    }

    [Fact]
    public void ClickOnCollapsedSection_RaisesExpandedChangedWithTrue()
    {
        bool? received = null;
        var cut = RenderComponent<CollapsibleSection>(p => p
            .Add(c => c.Title, "Stock")
            .Add(c => c.Expanded, false)
            .Add(c => c.ExpandedChanged, EventCallback.Factory.Create<bool>(this, v => received = v)));

        cut.Find("button").Click();

        Assert.True(received);
    }

    [Fact]
    public void ClickOnExpandedSection_RaisesExpandedChangedWithFalse()
    {
        bool? received = null;
        var cut = RenderComponent<CollapsibleSection>(p => p
            .Add(c => c.Title, "Stock")
            .Add(c => c.Expanded, true)
            .Add(c => c.ExpandedChanged, EventCallback.Factory.Create<bool>(this, v => received = v)));

        cut.Find("button").Click();

        Assert.False(received);
    }
}
