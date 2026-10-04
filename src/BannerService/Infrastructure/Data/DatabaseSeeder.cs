namespace BannerService.Infrastructure.Data
{
    using Domain.Entities;
    using Domain.Services;
    using Domain.ValueObjects;
    using Microsoft.EntityFrameworkCore;

    public class DatabaseSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            try
            {
                // Seed Roles
                await SeedRolesAsync(context);

                // Seed Subscription Plans
                await SeedSubscriptionPlansAsync(context);

                // Seed the product owner and the system shop
                await SeedSuperAdminAndSystemShopAsync(context);

                // Seed India Master Data
                await IndiaDataSeeder.SeedIndiaDataAsync(context);

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error seeding database", ex);
            }
        }

        private static async Task SeedRolesAsync(ApplicationDbContext context)
        {
            var roles = new List<Role>
            {
                new() { Id = "0", Name = "SuperAdmin", Description = "Permanent product owner with absolute control" },
                new() { Id = "1", Name = "Admin", Description = "Global administrator with full access" },
                new() { Id = "2", Name = "ShopOwner", Description = "Shop owner with shop management access" },
                new() { Id = "3", Name = "SalesExecutive", Description = "Sales executive with limited access" }
            };

            foreach (var role in roles)
            {
                var existingRole = await context.Set<Role>().FirstOrDefaultAsync(r => r.Id == role.Id);
                if (existingRole == null)
                {
                    context.Set<Role>().Add(role);
                }
                else if (existingRole.Name != role.Name || existingRole.Description != role.Description)
                {
                    existingRole.Name = role.Name;
                    existingRole.Description = role.Description;
                    context.Set<Role>().Update(existingRole);
                }
            }
        }

        private static async Task SeedSubscriptionPlansAsync(ApplicationDbContext context)
        {
            var plans = new List<SubscriptionPlan>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Basic",
                    Description = "Basic subscription plan - Perfect for getting started",
                    MonthlyPrice = 29.99m,
                    AnnualPrice = 299.99m,
                    Features = SubscriptionFeatures.CreateBasic(),
                    IsActive = true,
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Silver",
                    Description = "Silver subscription plan - Great for growing teams",
                    MonthlyPrice = 79.99m,
                    AnnualPrice = 799.99m,
                    Features = SubscriptionFeatures.CreateSilver(),
                    IsActive = true,
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Gold",
                    Description = "Gold subscription plan - Best for professional use",
                    MonthlyPrice = 199.99m,
                    AnnualPrice = 1999.99m,
                    Features = SubscriptionFeatures.CreateGold(),
                    IsActive = true,
                    DisplayOrder = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Platinum",
                    Description = "Platinum subscription plan - Enterprise-grade solution",
                    MonthlyPrice = 499.99m,
                    AnnualPrice = 4999.99m,
                    Features = SubscriptionFeatures.CreatePlatinum(),
                    IsActive = true,
                    DisplayOrder = 4,
                    CreatedAt = DateTime.UtcNow
                }
            };

            var existingPlans = await context.Set<SubscriptionPlan>().CountAsync();
            if (existingPlans == 0)
            {
                foreach (var plan in plans)
                {
                    context.Set<SubscriptionPlan>().Add(plan);
                }
            }
        }

        /// <summary>
        /// The product owner. Holds Admin as well as SuperAdmin so every screen and endpoint that accepts an administrator accepts the owner,
        /// and is put back in place at every start, so the account and its rights cannot be lost.
        /// </summary>
        private static async Task SeedSuperAdminAndSystemShopAsync(ApplicationDbContext context)
        {
            const string superAdminEmail = "26384sunilrana@gmail.com";

            var oldSystemUser = await context.Set<User>().Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == "system@bannersai.local");
            if (oldSystemUser != null)
            {
                context.Set<UserRole>().RemoveRange(oldSystemUser.UserRoles);
                context.Set<User>().Remove(oldSystemUser);
            }

            var owner = await context.Set<User>().Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == superAdminEmail);
            if (owner == null)
            {
                owner = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = superAdminEmail,
                    FirstName = "Sunil",
                    LastName = "Rana",
                    PasswordHash = "SUPER_ADMIN_PLACEHOLDER", // cannot sign in until --create-super-admin sets a password
                    EmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                };
                context.Set<User>().Add(owner);
            }

            owner.IsActive = true;
            foreach (var roleId in new[] { "0", "1" })
            {
                if (!owner.UserRoles.Any(r => r.RoleId == roleId))
                    context.Set<UserRole>().Add(new UserRole { UserId = owner.Id, RoleId = roleId });
            }
            await context.SaveChangesAsync();

            var systemShop = await context.Set<Shop>().FirstOrDefaultAsync(s => s.Name == "System Shop");
            if (systemShop == null)
            {
                systemShop = new Shop
                {
                    Id = Guid.NewGuid(),
                    Name = "System Shop",
                    Description = "System-managed shop for internal operations",
                    Status = ShopStatus.Active,
                    CreatedByUserId = Guid.Parse(owner.Id),
                    CreatedAt = DateTime.UtcNow
                };
                context.Set<Shop>().Add(systemShop);
            }

            // administrators belong to no shop; the system shop gives the login the shop id the sign-in token expects
            if (owner.ShopId != systemShop.Id.ToString())
                owner.ShopId = systemShop.Id.ToString();
        }
    }
}
