namespace BannerService.Infrastructure.Data;

using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

/// <summary>Creates or updates the Super Admin account (Sunil Rana). Safe to run again: it repairs the role and sets the new password.</summary>
public static class SuperAdminBootstrap
{
    private const string SuperAdminEmail = "26384sunilrana@gmail.com";

    public static async Task<string> RunAsync(ApplicationDbContext context, IPasswordHashService passwords, string[] args)
    {
        if (args.Length < 1)
            throw new ArgumentException("Usage: --create-super-admin <password> [first name] [last name]");

        var password = args[0];
        if (password.Length < 10)
            throw new ArgumentException("A super admin password needs at least 10 characters.");

        if (!await context.Roles.AnyAsync(r => r.Id == "0"))
            throw new InvalidOperationException("The database has no roles yet. Start the API once (or run --migrate) first.");

        var user = await context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == SuperAdminEmail);
        var created = user == null;

        if (user == null)
        {
            var systemShop = await context.Shops.FirstOrDefaultAsync(s => s.Name == "System Shop");
            user = new User
            {
                Email = SuperAdminEmail,
                FirstName = args.Length > 1 ? args[1] : "Sunil",
                LastName = args.Length > 2 ? args[2] : "Rana",
                ShopId = (systemShop?.Id ?? Guid.NewGuid()).ToString(),
                EmailVerified = true,
            };
            context.Users.Add(user);
        }

        user.PasswordHash = passwords.HashPassword(password);
        user.IsActive = true;
        user.IsLockedOut = false;
        user.LoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;

        // SuperAdmin sits on top of Admin: the owner holds both so every administrator screen and endpoint accepts the owner
        foreach (var roleId in new[] { "0", "1" })
        {
            if (!user.UserRoles.Any(r => r.RoleId == roleId))
                context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        if (string.IsNullOrEmpty(user.ShopId))
        {
            var systemShop = await context.Shops.FirstOrDefaultAsync(s => s.Name == "System Shop");
            if (systemShop != null) user.ShopId = systemShop.Id.ToString();
        }

        await context.SaveChangesAsync();
        return created ? $"Super Admin {SuperAdminEmail} created." : $"Super Admin {SuperAdminEmail} updated: password set, login switched on.";
    }
}
