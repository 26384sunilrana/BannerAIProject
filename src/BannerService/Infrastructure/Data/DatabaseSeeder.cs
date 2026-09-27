namespace BannerService.Infrastructure.Data
{
    using Domain.Entities;
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

                // Seed System User
                await SeedSystemUserAsync(context);

                // Seed System Shop
                await SeedSystemShopAsync(context);

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
                new() { Id = "1", Name = "Admin", Description = "System Administrator" },
                new() { Id = "2", Name = "ShopOwner", Description = "Shop Owner" },
                new() { Id = "3", Name = "SalesExecutive", Description = "Sales Executive" }
            };

            foreach (var role in roles)
            {
                var existingRole = await context.Set<Role>().FirstOrDefaultAsync(r => r.Id == role.Id);
                if (existingRole == null)
                {
                    context.Set<Role>().Add(role);
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

        private static async Task SeedSystemUserAsync(ApplicationDbContext context)
        {
            var systemUserExists = await context.Set<User>()
                .FirstOrDefaultAsync(u => u.Email == "system@bannersai.local");

            if (systemUserExists == null)
            {
                var systemUserId = Guid.NewGuid().ToString();
                var systemUser = new User
                {
                    Id = systemUserId,
                    Email = "system@bannersai.local",
                    FirstName = "System",
                    LastName = "User",
                    PasswordHash = "SYSTEM_ACCOUNT", // Never used for login
                    IsActive = true,
                    EmailVerified = true,
                    CreatedAt = DateTime.UtcNow
                };

                context.Set<User>().Add(systemUser);

                // Assign Admin role to system user
                var adminRole = await context.Set<Role>().FirstOrDefaultAsync(r => r.Id == "1");
                if (adminRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = systemUser.Id,
                        RoleId = adminRole.Id
                    };
                    context.Set<UserRole>().Add(userRole);
                }
            }
        }

        private static async Task SeedSystemShopAsync(ApplicationDbContext context)
        {
            var systemShopExists = await context.Set<Shop>()
                .FirstOrDefaultAsync(s => s.Name == "System Shop");

            if (systemShopExists == null)
            {
                var systemUser = await context.Set<User>()
                    .FirstOrDefaultAsync(u => u.Email == "system@bannersai.local");

                if (systemUser != null && Guid.TryParse(systemUser.Id, out var userId))
                {
                    var systemShop = new Shop
                    {
                        Id = Guid.NewGuid(),
                        Name = "System Shop",
                        Description = "System-managed shop for internal operations",
                        Status = ShopStatus.Active,
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Set<Shop>().Add(systemShop);
                }
            }
        }
    }
}
