using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace InventoryManagement.E2E;

[Collection("app")]
public class WarehouseFlowTests(AppFixture app)
{
    async Task<IPage> OpenLoginAsync()
    {
        var page = await app.Browser.NewPageAsync();
        await page.GotoAsync(app.BaseUrl + "/login");
        return page;
    }

    static async Task LogInAsync(IPage page, string email, string password)
    {
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Password").FillAsync(password);
        var dashboardLink = page.GetByRole(AriaRole.Link, new() { Name = "Dashboard" });
        for (var attempt = 0; attempt < 15; attempt++)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
            try
            {
                await dashboardLink.WaitForAsync(new() { Timeout = 1500 });
                return;
            }
            catch (TimeoutException) { /* try again */ }
        }
        await Expect(dashboardLink).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Login_Receive_ShowsStockInWarehouse()
    {
        var page = await OpenLoginAsync();
        await LogInAsync(page, "admin@demo.com", "Admin123!");

        await page.GetByRole(AriaRole.Link, new() { Name = "Warehouse" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Warehouse" })).ToBeVisibleAsync();

        await page.GetByLabel("Barcode").FillAsync("5901234123457");   // seeded product: Wireless Mouse (MOU-001)
        await page.GetByLabel("Quantity").FillAsync("10");
        await page.GetByLabel("Quantity").PressAsync("Tab");
        await page.GetByRole(AriaRole.Button, new() { Name = "Receive", Exact = true }).ClickAsync();

        await Expect(page.Locator(".toast.good")).ToContainTextAsync("Placed 10 pcs of Wireless Mouse");

        var row = page.Locator("tr", new() { HasText = "MOU-001" });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row).ToContainTextAsync("10");

        await page.CloseAsync();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsError()
    {
        var page = await OpenLoginAsync();
        await page.GetByLabel("Email").FillAsync("admin@demo.com");
        await page.GetByLabel("Password").FillAsync("wrong-password");

        for (var attempt = 0; attempt < 15; attempt++)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
            if (await page.GetByRole(AriaRole.Alert).CountAsync() > 0) break;
            await page.WaitForTimeoutAsync(500);
        }

        await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Invalid email or password");
        await page.CloseAsync();
    }
}
