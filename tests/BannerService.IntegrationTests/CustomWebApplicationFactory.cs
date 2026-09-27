using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BannerService.Infrastructure.Data;
using BannerService.Domain.Services;
using BannerService.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace BannerService.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration tests.
/// Overrides database to use InMemory and seeds test data.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production SQL Server DbContext
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
            });

            // Build the service provider and seed test data
            var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Ensure database is created
                dbContext.Database.EnsureCreated();

                // Seed test data
                SeedTestData(dbContext);
            }
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Seeds minimal test data (roles, users) into the in-memory database.
    /// </summary>
    private static void SeedTestData(ApplicationDbContext context)
    {
        // Check if data already exists
        if (context.Roles.Any() || context.Users.Any())
        {
            return;
        }

        // Seed roles
        var adminRole = new Role { Name = "Admin", Description = "Administrator role" };
        var shopOwnerRole = new Role { Name = "ShopOwner", Description = "Shop owner role" };
        var userRole = new Role { Name = "User", Description = "Regular user role" };

        context.Roles.AddRange(adminRole, shopOwnerRole, userRole);

        // Seed test users
        var passwordHashService = new PasswordHashService();
        var testPassword = "TestPassword123!";

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@test.com",
            FullName = "Test Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        adminUser.SetPasswordHash(passwordHashService.HashPassword(testPassword));

        var shopOwnerUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "shopowner@test.com",
            FullName = "Test Shop Owner",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        shopOwnerUser.SetPasswordHash(passwordHashService.HashPassword(testPassword));

        context.Users.AddRange(adminUser, shopOwnerUser);

        // Assign roles
        context.UserRoles.AddRange(
            new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id },
            new UserRole { UserId = shopOwnerUser.Id, RoleId = shopOwnerRole.Id }
        );

        context.SaveChanges();
    }

    /// <summary>
    /// Generates a JWT token for a test user.
    /// Used for testing [Authorize] endpoints.
    /// </summary>
    public string GenerateTestJwt(string email = "shopowner@test.com")
    {
        var services = Services.CreateScope().ServiceProvider;
        var jwtTokenService = services.GetRequiredService<IJwtTokenService>();
        var userRepository = services.GetRequiredService<IUserRepository>();

        var user = userRepository.GetByEmailAsync(email).Result;
        if (user == null)
        {
            throw new InvalidOperationException($"Test user with email {email} not found");
        }

        var roles = userRepository.GetUserRolesAsync(user.Id).Result;
        return jwtTokenService.GenerateToken(user, roles);
    }
}
