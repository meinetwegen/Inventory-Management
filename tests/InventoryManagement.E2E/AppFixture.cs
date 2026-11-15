using System.Diagnostics;
using Microsoft.Playwright;

namespace InventoryManagement.E2E;

// Starts the real web app on a free port with an empty temporary SQLite database,
// and opens a Chromium browser. One instance is shared by all tests in the collection.
public class AppFixture : IAsyncLifetime
{
    const string Url = "http://127.0.0.1:5199";
    Process? app;
    string? dbFile;

    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl => Url;

    public async Task InitializeAsync()
    {
        var root = FindSolutionRoot();
        dbFile = Path.Combine(Path.GetTempPath(), $"inventory-e2e-{Guid.NewGuid():N}.db");

        var psi = new ProcessStartInfo("dotnet",
            $"run --project \"{Path.Combine(root, "src", "InventoryManagement.Web")}\" --no-launch-profile --urls {Url}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        psi.Environment["ConnectionStrings__Default"] = $"Data Source={dbFile}";
        psi.Environment["SEED_DEMO"] = "false";
        app = Process.Start(psi)!;
        // drain output so the child process never blocks on a full pipe
        app.OutputDataReceived += (_, _) => { };
        app.ErrorDataReceived += (_, _) => { };
        app.BeginOutputReadLine();
        app.BeginErrorReadLine();

        await WaitUntilReadyAsync();

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        // E2E_HEADED=1 shows the browser window and slows the steps down, so the test can be watched
        var headed = Environment.GetEnvironmentVariable("E2E_HEADED") == "1";
        Browser = await Playwright.Chromium.LaunchAsync(new() { Headless = !headed, SlowMo = headed ? 600 : 0 });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.CloseAsync();
        Playwright?.Dispose();
        if (app is { HasExited: false }) app.Kill(entireProcessTree: true);
        app?.Dispose();
        try { if (dbFile is not null) File.Delete(dbFile); } catch { /* file may still be locked, ignore */ }
    }

    async Task WaitUntilReadyAsync()
    {
        using var http = new HttpClient();
        var deadline = DateTime.UtcNow.AddSeconds(150);   // first run also compiles the app
        while (DateTime.UtcNow < deadline)
        {
            if (app!.HasExited) throw new InvalidOperationException("The web app exited during start-up.");
            try
            {
                var response = await http.GetAsync(Url + "/login");
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { /* not listening yet */ }
            await Task.Delay(1000);
        }
        throw new System.TimeoutException("The web app did not start in time.");
    }

    static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "InventoryManagement.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("InventoryManagement.sln not found above the test output folder.");
    }
}

[CollectionDefinition("app")]
public class AppCollection : ICollectionFixture<AppFixture> { }
