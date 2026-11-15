using Bunit;
using Bunit.TestDoubles;
using InventoryManagement.Web.Components.Shared;
using InventoryManagement.Web.Models;
using InventoryManagement.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Tests;

public class RoleGuardTests : TestContext
{
    readonly SessionState session = new();

    public RoleGuardTests() => Services.AddSingleton(session);

    [Fact]
    public void NotSignedIn_ShowsRedirectText_AndNavigatesToLogin()
    {
        var cut = RenderComponent<RoleGuard>(p => p.AddChildContent("<p id=\"secret\">Secret</p>"));

        Assert.Empty(cut.FindAll("#secret"));
        Assert.Contains("Redirecting to login", cut.Markup);
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        Assert.EndsWith("login", nav.Uri);
    }

    [Fact]
    public void SignedIn_WithoutRoleRestriction_ShowsContent()
    {
        session.Login(new SessionUser(1, "op@test.com", Role.Operator));

        var cut = RenderComponent<RoleGuard>(p => p.AddChildContent("<p id=\"secret\">Secret</p>"));

        Assert.Single(cut.FindAll("#secret"));
    }

    [Fact]
    public void SignedIn_WithRoleNotAllowed_ShowsAccessDenied()
    {
        session.Login(new SessionUser(1, "op@test.com", Role.Operator));

        var cut = RenderComponent<RoleGuard>(p => p
            .Add(c => c.Roles, new[] { Role.Administrator })
            .AddChildContent("<p id=\"secret\">Secret</p>"));

        Assert.Empty(cut.FindAll("#secret"));
        Assert.Contains("Access denied", cut.Markup);
    }

    [Fact]
    public void SignedIn_WithAllowedRole_ShowsContent()
    {
        session.Login(new SessionUser(1, "admin@test.com", Role.Administrator));

        var cut = RenderComponent<RoleGuard>(p => p
            .Add(c => c.Roles, new[] { Role.Administrator, Role.Manager })
            .AddChildContent("<p id=\"secret\">Secret</p>"));

        Assert.Single(cut.FindAll("#secret"));
        Assert.DoesNotContain("Access denied", cut.Markup);
    }
}
