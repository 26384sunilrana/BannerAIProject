namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class UserRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private UserRepository _repository;
    private readonly string _testShopId = Guid.NewGuid().ToString();
    private readonly string _otherShopId = Guid.NewGuid().ToString();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new UserRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var user1 = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user1@test.com",
            FirstName = "John",
            LastName = "Doe",
            ShopId = _testShopId,
            IsActive = true,
            EmailVerified = true,
            PasswordHash = "hash1"
        };

        var user2 = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user2@test.com",
            FirstName = "Jane",
            LastName = "Smith",
            ShopId = _testShopId,
            IsActive = true,
            EmailVerified = false,
            PasswordHash = "hash2"
        };

        var inactiveUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "inactive@test.com",
            FirstName = "Inactive",
            LastName = "User",
            ShopId = _testShopId,
            IsActive = false,
            PasswordHash = "hash3"
        };

        var otherShopUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "other@test.com",
            FirstName = "Other",
            LastName = "Shop",
            ShopId = _otherShopId,
            IsActive = true,
            PasswordHash = "hash4"
        };

        _context.Users.AddRange(user1, user2, inactiveUser, otherShopUser);
        await _context.SaveChangesAsync();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var user = _context.Users.First(u => u.ShopId == _testShopId);

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeUserRoles()
    {
        // Arrange
        var user = _context.Users.First(u => u.ShopId == _testShopId);

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.UserRoles);
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        var email = "user1@test.com";

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@test.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldBeCaseInsensitive()
    {
        // Act
        var result = await _repository.GetByEmailAsync("USER1@TEST.COM");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user1@test.com", result.Email);
    }

    #endregion

    #region GetByVerificationTokenAsync Tests

    [Fact]
    public async Task GetByVerificationTokenAsync_WithValidToken_ShouldReturnUser()
    {
        // Arrange
        var user = _context.Users.First();
        user.VerificationToken = "test-token-123";
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByVerificationTokenAsync("test-token-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetByVerificationTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByVerificationTokenAsync("invalid-token");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByPasswordResetTokenAsync Tests

    [Fact]
    public async Task GetByPasswordResetTokenAsync_WithValidToken_ShouldReturnUser()
    {
        // Arrange
        var user = _context.Users.First();
        user.PasswordResetToken = "reset-token-123";
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPasswordResetTokenAsync("reset-token-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
    }

    [Fact]
    public async Task GetByPasswordResetTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByPasswordResetTokenAsync("invalid-reset-token");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByShopIdAsync Tests

    [Fact]
    public async Task GetByShopIdAsync_WithValidShopId_ShouldReturnActiveUsers()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(_testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, u =>
        {
            Assert.Equal(_testShopId, u.ShopId);
            Assert.True(u.IsActive);
        });
    }

    [Fact]
    public async Task GetByShopIdAsync_ShouldNotIncludeInactiveUsers()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(_testShopId);

        // Assert
        Assert.DoesNotContain(results, u => u.Email == "inactive@test.com");
    }

    [Fact]
    public async Task GetByShopIdAsync_WithNoUsers_ShouldReturnEmptyList()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region GetAllActiveAsync Tests

    [Fact]
    public async Task GetAllActiveAsync_ShouldReturnOnlyActiveUsers()
    {
        // Act
        var results = await _repository.GetAllActiveAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, u => Assert.True(u.IsActive));
    }

    [Fact]
    public async Task GetAllActiveAsync_ShouldNotIncludeInactiveUsers()
    {
        // Act
        var results = await _repository.GetAllActiveAsync();

        // Assert
        Assert.DoesNotContain(results, u => u.Email == "inactive@test.com");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidUser_ShouldAddToDatabase()
    {
        // Arrange
        var newUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "newuser@test.com",
            FirstName = "New",
            LastName = "User",
            ShopId = _testShopId,
            IsActive = true,
            PasswordHash = "newhash"
        };

        // Act
        var result = await _repository.CreateAsync(newUser);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Users.FindAsync(newUser.Id);
        Assert.NotNull(saved);
        Assert.Equal("newuser@test.com", saved.Email);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetTimestamps()
    {
        // Arrange
        var newUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "timestamp@test.com",
            ShopId = _testShopId,
            PasswordHash = "hash"
        };

        // Act
        await _repository.CreateAsync(newUser);

        // Assert
        var saved = await _context.Users.FindAsync(newUser.Id);
        Assert.True(saved.CreatedAt > DateTime.MinValue);
        Assert.True(saved.UpdatedAt > DateTime.MinValue);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidUser_ShouldUpdateDatabase()
    {
        // Arrange
        var user = _context.Users.First(u => u.ShopId == _testShopId);
        user.FirstName = "Updated";

        // Act
        await _repository.UpdateAsync(user);

        // Assert
        var updated = await _context.Users.FindAsync(user.Id);
        Assert.Equal("Updated", updated.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateModificationTime()
    {
        // Arrange
        var user = _context.Users.First(u => u.ShopId == _testShopId);
        var originalTime = user.UpdatedAt;
        System.Threading.Thread.Sleep(10);
        user.FirstName = "Modified";

        // Act
        await _repository.UpdateAsync(user);

        // Assert
        var updated = await _context.Users.FindAsync(user.Id);
        Assert.True(updated.UpdatedAt > originalTime);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemoveFromDatabase()
    {
        // Arrange
        var user = _context.Users.First(u => u.ShopId == _testShopId && u.IsActive);

        // Act
        var result = await _repository.DeleteAsync(user.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Users.FindAsync(user.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingEmail_ShouldReturnTrue()
    {
        // Act
        var result = await _repository.ExistsAsync("user1@test.com");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonexistentEmail_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync("nonexistent@test.com");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetCountByShopAsync Tests

    [Fact]
    public async Task GetCountByShopAsync_WithValidShop_ShouldReturnCorrectCount()
    {
        // Act
        var count = await _repository.GetCountByShopAsync(_testShopId);

        // Assert
        Assert.Equal(2, count);  // user1 and user2 are active
    }

    [Fact]
    public async Task GetCountByShopAsync_ShouldOnlyCountActiveUsers()
    {
        // Act
        var count = await _repository.GetCountByShopAsync(_testShopId);

        // Assert
        Assert.Equal(2, count); // the two active users of the shop; the deactivated one and the other shop's user are not counted
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithEmailMatch_ShouldReturnUser()
    {
        // Act
        var results = await _repository.SearchAsync("user1", _testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, u => u.Email.Contains("user1"));
    }

    [Fact]
    public async Task SearchAsync_WithFirstNameMatch_ShouldReturnUser()
    {
        // Act
        var results = await _repository.SearchAsync("John", _testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, u => u.FirstName == "John");
    }

    [Fact]
    public async Task SearchAsync_WithLastNameMatch_ShouldReturnUser()
    {
        // Act
        var results = await _repository.SearchAsync("Smith", _testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, u => u.LastName == "Smith");
    }

    [Fact]
    public async Task SearchAsync_ShouldBeCaseInsensitive()
    {
        // Act
        var results = await _repository.SearchAsync("JOHN", _testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, u => u.FirstName == "John");
    }

    [Fact]
    public async Task SearchAsync_ShouldNotReturnInactiveUsers()
    {
        // Act
        var results = await _repository.SearchAsync("inactive", _testShopId);

        // Assert
        Assert.Empty(results);
    }

    #endregion
}
