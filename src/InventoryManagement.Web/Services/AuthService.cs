using InventoryManagement.Web.Data;
using InventoryManagement.Web.Logic;
using InventoryManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Web.Services;

public class AuthService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<Result<SessionUser>> LoginAsync(string? email, string? password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            return Result.Fail<SessionUser>("Enter email and password");

        await using var db = await factory.CreateDbContextAsync();
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower() && u.IsActive);

        return user is not null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)
            ? Result.Ok(new SessionUser(user.Id, user.Email, user.Role))
            : Result.Fail<SessionUser>("Invalid email or password");
    }
}
