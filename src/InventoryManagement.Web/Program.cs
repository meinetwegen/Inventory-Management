using InventoryManagement.Web.Components;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var connection = builder.Configuration.GetConnectionString("Default") ?? "Data Source=inventory.db";
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(connection));

// scoped = one instance per user circuit
builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<InventoryState>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<BinService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ReportService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db, Environment.GetEnvironmentVariable("SEED_DEMO") == "true");
}

app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();

public partial class Program { }
