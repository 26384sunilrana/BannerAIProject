namespace BannerService.Infrastructure.Data;

using Domain.Entities;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

/// <summary>Makes the first administrator login (see <c>--create-admin</c>). Safe to run again: it repairs the role and sets the new password.</summary>
public static class AdminBootstrap
{
    public static async Task<string> RunAsync(ApplicationDbContext context, IPasswordHashService passwords, string[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("Usage: --create-admin <email> <password> [first name] [last name]");

        var email = args[0].Trim().ToLowerInvariant();
        var password = args[1];
        if (!email.Contains('@') || email.Length > 200) throw new ArgumentException("That does not look like an e-mail address.");
        if (password.Length < 10) throw new ArgumentException("An administrator password needs at least 10 characters.");
        if (email == "26384sunilrana@gmail.com") throw new ArgumentException("That is the product owner: use --create-super-admin.");

        if (!await context.Roles.AnyAsync(r => r.Id == "1"))
            throw new InvalidOperationException("The database has no roles yet. Start the API once (or run --migrate) first.");

        var user = await context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == email);
        var created = user == null;
        if (user == null)
        {
            // administrators belong to no shop; the system shop gives the login the shop id the sign-in token expects
            var systemShop = await context.Shops.FirstOrDefaultAsync(s => s.Name == "System Shop");
            user = new User
            {
                Email = email,
                FirstName = args.Length > 2 ? args[2] : "Admin",
                LastName = args.Length > 3 ? args[3] : "User",
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

        if (!user.UserRoles.Any(r => r.RoleId == "1"))
            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = "1" });

        await context.SaveChangesAsync();
        return created ? $"Administrator {email} created." : $"Administrator {email} updated: password set, login switched on.";
    }
}
