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

    static async Task WaitForConnectionAsync(IPage page)
    {
        var loginButton = page.GetByRole(AriaRole.Button, new() { Name = "Log in" });
        var alert = page.GetByRole(AriaRole.Alert);
        for (var attempt = 0; attempt < 40; attempt++)
        {
            await loginButton.ClickAsync();
            if (await alert.CountAsync() > 0) return;
            await page.WaitForTimeoutAsync(500);
        }
        throw new InvalidOperationException("The Blazor connection did not become ready.");
    }

    static async Task FillLoginFormAsync(IPage page, string email, string password)
    {
        await WaitForConnectionAsync(page);
        await page.GetByLabel("Email").FillAsync(email);
        await page.GetByLabel("Email").PressAsync("Tab");
        await page.GetByLabel("Password").FillAsync(password);
        await page.GetByLabel("Password").PressAsync("Tab");
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
    }

    static async Task LogInAsync(IPage page, string email, string password)
    {
        await FillLoginFormAsync(page, email, password);
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Dashboard" }))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Fact]
    public async Task Login_Receive_ShowsStockInWarehouse()
    {
        var page = await OpenLoginAsync();
        await LogInAsync(page, "admin@demo.com", "Admin123!");

        await page.GetByRole(AriaRole.Link, new() { Name = "Warehouse" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Warehouse" })).ToBeVisibleAsync();

        await page.GetByLabel("Barcode").FillAsync("5901234123457");   // seeded product: Wireless Mouse (MOU-001)
        await page.GetByLabel("Barcode").PressAsync("Tab");
        await page.GetByLabel("Quantity").FillAsync("10");
        await page.GetByLabel("Quantity").PressAsync("Tab");
        await page.GetByRole(AriaRole.Button, new() { Name = "Receive", Exact = true }).ClickAsync();

        await Expect(page.Locator(".toast.good")).ToContainTextAsync("Placed 10 pcs of Wireless Mouse");

        var row = page.Locator("tr", new() { HasText = "MOU-001" });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row).ToContainTextAsync("10");

        var folder = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(folder);
        await page.ScreenshotAsync(new() { Path = Path.Combine(folder, "warehouse-after-receive.png"), FullPage = true });

        await page.CloseAsync();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsError()
    {
        var page = await OpenLoginAsync();
        await FillLoginFormAsync(page, "admin@demo.com", "wrong-password");

        await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Invalid email or password");
        await page.CloseAsync();
    }
}
