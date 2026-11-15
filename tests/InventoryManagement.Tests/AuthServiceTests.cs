using InventoryManagement.Web.Models;
using InventoryManagement.Web.Services;

namespace InventoryManagement.Tests;

// Integration tests: AuthService against a real SQLite database in memory (TestFactory).
public class AuthServiceTests
{
    const string Password = "Secret123!";

    static (AuthService Svc, TestFactory F) Create()
    {
        var f = new TestFactory();
        using var db = f.CreateDbContext();
        db.Users.AddRange(
            new AppUser { Email = "manager@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password), Role = Role.Manager },
            new AppUser { Email = "gone@test.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password), Role = Role.Operator, IsActive = false });
        db.SaveChanges();
        return (new AuthService(f), f);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsSessionUserWithRole()
    {
        var (svc, _) = Create();

        var r = await svc.LoginAsync("manager@test.com", Password);

        Assert.True(r.IsOk);
        var user = r.Unwrap();
        Assert.Equal("manager@test.com", user.Email);
        Assert.Equal(Role.Manager, user.Role);
    }

    [Fact]
    public async Task Login_IgnoresCaseAndSurroundingSpacesInEmail()
    {
        var (svc, _) = Create();

        var r = await svc.LoginAsync("  Manager@TEST.com ", Password);

        Assert.True(r.IsOk);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Fails()
    {
        var (svc, _) = Create();

        var r = await svc.LoginAsync("manager@test.com", "wrong");

        Assert.False(r.IsOk);
        Assert.Equal("Invalid email or password", r.Message);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_GivesTheSameMessageAsWrongPassword()
    {
        var (svc, _) = Create();

        var r = await svc.LoginAsync("nobody@test.com", Password);

        // the same text for both cases, so the form does not reveal which emails exist
        Assert.Equal("Invalid email or password", r.Message);
    }

    [Fact]
    public async Task Login_WithDisabledAccount_Fails()
    {
        var (svc, _) = Create();

        var r = await svc.LoginAsync("gone@test.com", Password);

        Assert.False(r.IsOk);
        Assert.Equal("Invalid email or password", r.Message);
    }

    [Theory]
    [InlineData(null, "x")]
    [InlineData("   ", "x")]
    [InlineData("manager@test.com", "")]
    [InlineData("manager@test.com", null)]
    public async Task Login_WithMissingInput_AsksForEmailAndPassword(string? email, string? password)
    {
        var (svc, _) = Create();

        var r = await svc.LoginAsync(email, password);

        Assert.False(r.IsOk);
        Assert.Equal("Enter email and password", r.Message);
    }
}
