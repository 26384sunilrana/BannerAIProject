using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BannerService.Application.Tests.Authentication
{
    public class UserRepositoryTests : IAsyncLifetime
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private ApplicationDbContext _context = null!;
        private UserRepository _userRepository = null!;

        public UserRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        public async Task InitializeAsync()
        {
            _context = new ApplicationDbContext(_options);
            await _context.Database.EnsureCreatedAsync();
            _userRepository = new UserRepository(_context);
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        [Fact]
        public async Task CreateAsync_WithValidUser_CreatesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Id = "user-123",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };

            // Act
            var createdUser = await _userRepository.CreateAsync(user);

            // Assert
            Assert.NotNull(createdUser);
            Assert.NotNull(createdUser.CreatedAt);
            var retrievedUser = await _userRepository.GetByIdAsync(user.Id);
            Assert.NotNull(retrievedUser);
            Assert.Equal("test@example.com", retrievedUser.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_WithExistingEmail_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                Id = "user-123",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            await _userRepository.CreateAsync(user);

            // Act
            var retrievedUser = await _userRepository.GetByEmailAsync("test@example.com");

            // Assert
            Assert.NotNull(retrievedUser);
            Assert.Equal("user-123", retrievedUser.Id);
            Assert.Equal("John", retrievedUser.FirstName);
        }

        [Fact]
        public async Task GetByEmailAsync_WithNonexistentEmail_ReturnsNull()
        {
            // Act
            var retrievedUser = await _userRepository.GetByEmailAsync("nonexistent@example.com");

            // Assert
            Assert.Null(retrievedUser);
        }

        [Fact]
        public async Task ExistsAsync_WithExistingEmail_ReturnsTrue()
        {
            // Arrange
            var user = new User
            {
                Id = "user-123",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            await _userRepository.CreateAsync(user);

            // Act
            var exists = await _userRepository.ExistsAsync("test@example.com");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_WithNonexistentEmail_ReturnsFalse()
        {
            // Act
            var exists = await _userRepository.ExistsAsync("nonexistent@example.com");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task UpdateAsync_WithValidUser_UpdatesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Id = "user-123",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            await _userRepository.CreateAsync(user);

            // Act
            user.FirstName = "Jane";
            var updatedUser = await _userRepository.UpdateAsync(user);

            // Assert
            Assert.Equal("Jane", updatedUser.FirstName);
            var retrievedUser = await _userRepository.GetByIdAsync(user.Id);
            Assert.Equal("Jane", retrievedUser!.FirstName);
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_DeletesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Id = "user-123",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            await _userRepository.CreateAsync(user);

            // Act
            var deleted = await _userRepository.DeleteAsync("user-123");

            // Assert
            Assert.True(deleted);
            var retrievedUser = await _userRepository.GetByIdAsync("user-123");
            Assert.Null(retrievedUser);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsFalse()
        {
            // Act
            var deleted = await _userRepository.DeleteAsync("nonexistent");

            // Assert
            Assert.False(deleted);
        }

        [Fact]
        public async Task GetByShopIdAsync_WithValidShopId_ReturnsUsersForShop()
        {
            // Arrange
            var user1 = new User
            {
                Id = "user-1",
                Email = "user1@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            var user2 = new User
            {
                Id = "user-2",
                Email = "user2@example.com",
                PasswordHash = "hash",
                FirstName = "Jane",
                LastName = "Smith",
                ShopId = "shop-123"
            };
            var user3 = new User
            {
                Id = "user-3",
                Email = "user3@example.com",
                PasswordHash = "hash",
                FirstName = "Bob",
                LastName = "Johnson",
                ShopId = "shop-456"
            };

            await _userRepository.CreateAsync(user1);
            await _userRepository.CreateAsync(user2);
            await _userRepository.CreateAsync(user3);

            // Act
            var users = await _userRepository.GetByShopIdAsync("shop-123");

            // Assert
            Assert.Equal(2, users.Count);
            Assert.All(users, u => Assert.Equal("shop-123", u.ShopId));
        }

        [Fact]
        public async Task GetCountByShopAsync_ReturnsCorrectCount()
        {
            // Arrange
            var user1 = new User
            {
                Id = "user-1",
                Email = "user1@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            var user2 = new User
            {
                Id = "user-2",
                Email = "user2@example.com",
                PasswordHash = "hash",
                FirstName = "Jane",
                LastName = "Smith",
                ShopId = "shop-123"
            };

            await _userRepository.CreateAsync(user1);
            await _userRepository.CreateAsync(user2);

            // Act
            var count = await _userRepository.GetCountByShopAsync("shop-123");

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public async Task SearchAsync_WithSearchTerm_ReturnsMatchingUsers()
        {
            // Arrange
            var user1 = new User
            {
                Id = "user-1",
                Email = "john.doe@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            var user2 = new User
            {
                Id = "user-2",
                Email = "jane.smith@example.com",
                PasswordHash = "hash",
                FirstName = "Jane",
                LastName = "Smith",
                ShopId = "shop-123"
            };

            await _userRepository.CreateAsync(user1);
            await _userRepository.CreateAsync(user2);

            // Act
            var results = await _userRepository.SearchAsync("John", "shop-123");

            // Assert
            Assert.Single(results);
            Assert.Equal("user-1", results[0].Id);
        }
    }

    public class RoleRepositoryTests : IAsyncLifetime
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private ApplicationDbContext _context = null!;
        private RoleRepository _roleRepository = null!;

        public RoleRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        public async Task InitializeAsync()
        {
            _context = new ApplicationDbContext(_options);
            await _context.Database.EnsureCreatedAsync();
            _roleRepository = new RoleRepository(_context);
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        [Fact]
        public async Task GetByNameAsync_WithValidName_ReturnsRole()
        {
            // Arrange
            var role = new Role
            {
                Id = "1",
                Name = "Admin",
                IsActive = true
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Act
            var retrievedRole = await _roleRepository.GetByNameAsync("Admin");

            // Assert
            Assert.NotNull(retrievedRole);
            Assert.Equal("Admin", retrievedRole.Name);
        }

        [Fact]
        public async Task GetByNameAsync_WithInactiveRole_ReturnsNull()
        {
            // Arrange
            var role = new Role
            {
                Id = "1",
                Name = "Admin",
                IsActive = false
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Act
            var retrievedRole = await _roleRepository.GetByNameAsync("Admin");

            // Assert
            Assert.Null(retrievedRole);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOnlyActiveRoles()
        {
            // Arrange
            var role1 = new Role { Id = "1", Name = "Admin", IsActive = true };
            var role2 = new Role { Id = "2", Name = "ShopOwner", IsActive = true };
            var role3 = new Role { Id = "3", Name = "Inactive", IsActive = false };

            _context.Roles.AddRange(role1, role2, role3);
            await _context.SaveChangesAsync();

            // Act
            var roles = await _roleRepository.GetAllAsync();

            // Assert
            Assert.Equal(2, roles.Count);
            Assert.All(roles, r => Assert.True(r.IsActive));
        }

        [Fact]
        public async Task AssignRoleAsync_WithValidUserAndRole_AssignsSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Id = "user-1",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            var role = new Role { Id = "1", Name = "Admin", IsActive = true };

            _context.Users.Add(user);
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Act
            var result = await _roleRepository.AssignRoleAsync("user-1", "1");

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.UserRoles);
            Assert.Equal("1", result.UserRoles[0].RoleId);
        }

        [Fact]
        public async Task RemoveRoleAsync_WithValidAssignment_RemovesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Id = "user-1",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            var role = new Role { Id = "1", Name = "Admin", IsActive = true };
            var userRole = new UserRole { UserId = "user-1", RoleId = "1" };

            _context.Users.Add(user);
            _context.Roles.Add(role);
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            // Act
            var removed = await _roleRepository.RemoveRoleAsync("user-1", "1");

            // Assert
            Assert.True(removed);
            var userRoles = _context.UserRoles.Where(ur => ur.UserId == "user-1").ToList();
            Assert.Empty(userRoles);
        }

        [Fact]
        public async Task UserHasRoleAsync_WithValidRole_ReturnsTrue()
        {
            // Arrange
            var user = new User
            {
                Id = "user-1",
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };
            var role = new Role { Id = "1", Name = "Admin", IsActive = true };
            var userRole = new UserRole { UserId = "user-1", RoleId = "1" };

            _context.Users.Add(user);
            _context.Roles.Add(role);
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            // Act
            var hasRole = await _roleRepository.UserHasRoleAsync("user-1", "Admin");

            // Assert
            Assert.True(hasRole);
        }

        [Fact]
        public async Task UserHasRoleAsync_WithoutRole_ReturnsFalse()
        {
            // Act
            var hasRole = await _roleRepository.UserHasRoleAsync("nonexistent", "Admin");

            // Assert
            Assert.False(hasRole);
        }
    }

    public class RefreshTokenRepositoryTests : IAsyncLifetime
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;
        private ApplicationDbContext _context = null!;
        private RefreshTokenRepository _refreshTokenRepository = null!;

        public RefreshTokenRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        public async Task InitializeAsync()
        {
            _context = new ApplicationDbContext(_options);
            await _context.Database.EnsureCreatedAsync();
            _refreshTokenRepository = new RefreshTokenRepository(_context);
        }

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync();
        }

        [Fact]
        public async Task AddAsync_WithValidToken_CreatesSuccessfully()
        {
            // Arrange
            var token = new RefreshToken
            {
                UserId = "user-123",
                Token = "refresh-token-123",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0"
            };

            // Act
            var result = await _refreshTokenRepository.AddAsync(token);

            // Assert
            Assert.NotNull(result.Id);
            var retrieved = await _refreshTokenRepository.GetByTokenAsync("refresh-token-123");
            Assert.NotNull(retrieved);
        }

        [Fact]
        public async Task GetByTokenAsync_WithValidToken_ReturnsToken()
        {
            // Arrange
            var token = new RefreshToken
            {
                Id = "rt-123",
                UserId = "user-123",
                Token = "refresh-token-123",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0"
            };
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();

            // Act
            var retrieved = await _refreshTokenRepository.GetByTokenAsync("refresh-token-123");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("user-123", retrieved.UserId);
        }

        [Fact]
        public async Task GetActiveByUserIdAsync_ReturnsOnlyActiveTokens()
        {
            // Arrange
            var userId = "user-123";
            var activeToken = new RefreshToken
            {
                Id = "rt-1",
                UserId = userId,
                Token = "active-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0"
            };
            var revokedToken = new RefreshToken
            {
                Id = "rt-2",
                UserId = userId,
                Token = "revoked-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = true,
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0"
            };
            var expiredToken = new RefreshToken
            {
                Id = "rt-3",
                UserId = userId,
                Token = "expired-token",
                ExpiresAt = DateTime.UtcNow.AddDays(-1),
                IsRevoked = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0"
            };

            _context.RefreshTokens.AddRange(activeToken, revokedToken, expiredToken);
            await _context.SaveChangesAsync();

            // Act
            var active = await _refreshTokenRepository.GetActiveByUserIdAsync(userId);

            // Assert
            Assert.Single(active);
            Assert.Equal("active-token", active[0].Token);
        }

        [Fact]
        public async Task DeleteExpiredTokensAsync_RemovesExpiredTokens()
        {
            // Arrange
            var expiredToken = new RefreshToken
            {
                Id = "rt-1",
                UserId = "user-123",
                Token = "expired-token",
                ExpiresAt = DateTime.UtcNow.AddDays(-1),
                IsRevoked = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0"
            };
            var validToken = new RefreshToken
            {
                Id = "rt-2",
                UserId = "user-123",
                Token = "valid-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0"
            };

            _context.RefreshTokens.AddRange(expiredToken, validToken);
            await _context.SaveChangesAsync();

            // Act
            var deletedCount = await _refreshTokenRepository.DeleteExpiredTokensAsync();

            // Assert
            Assert.Equal(1, deletedCount);
            var remaining = _context.RefreshTokens.Count();
            Assert.Equal(1, remaining);
        }
    }
}
