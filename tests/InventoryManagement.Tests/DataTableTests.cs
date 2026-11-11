using Bunit;
using InventoryManagement.Web.Components.Shared;
using Microsoft.AspNetCore.Components;

namespace InventoryManagement.Tests;

public class DataTableTests : TestContext
{
    [Fact]
    public void ShowsRows()
    {
        var cut = RenderComponent<DataTable<string>>(p => p
            .Add(c => c.Items, new[] { "one", "two" })
            .Add(c => c.RowTemplate, (RenderFragment<string>)(s => b => b.AddContent(0, $"<td>{s}</td>"))));
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public void ShowsEmptyMessage_WhenNoItems()
    {
        var cut = RenderComponent<DataTable<string>>(p => p
            .Add(c => c.Items, Array.Empty<string>())
            .Add(c => c.RowTemplate, (RenderFragment<string>)(s => b => { })));
        Assert.Contains("No data", cut.Markup);
    }
}
