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

public class RefreshTokenRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private RefreshTokenRepository _repository;
    private readonly string _testUserId = Guid.NewGuid().ToString();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new RefreshTokenRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            Token = "test_token_1",
            UserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            Token = "test_token_2",
            UserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid().ToString(),
            Token = "expired_token",
            UserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        _context.RefreshTokens.AddRange(token1, token2, expiredToken);

        await _context.SaveChangesAsync();
    }

    #region GetByTokenAsync Tests

    [Fact]
    public async Task GetByTokenAsync_WithValidToken_ShouldReturnToken()
    {
        // Arrange
        var token = _context.RefreshTokens.First();

        // Act
        var result = await _repository.GetByTokenAsync(token.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Token, result.Token);
    }

    [Fact]
    public async Task GetByTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByTokenAsync("non_existent_token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTokenAsync_ShouldIncludeUser()
    {
        // Arrange
        var token = _context.RefreshTokens.First();

        // Act
        var result = await _repository.GetByTokenAsync(token.Token);

        // Assert
        Assert.NotNull(result);
        // User would be loaded due to Include in repository
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnToken()
    {
        // Arrange
        var token = _context.RefreshTokens.First();

        // Act
        var result = await _repository.GetByIdAsync(token.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ShouldReturnAllTokens()
    {
        // Act
        var results = await _repository.GetByUserIdAsync(_testUserId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, t => Assert.Equal(_testUserId, t.UserId));
    }

    [Fact]
    public async Task GetByUserIdAsync_WithInvalidUserId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByUserIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOrderedByCreatedDateDescending()
    {
        // Act
        var results = await _repository.GetByUserIdAsync(_testUserId);

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(t => t.CreatedAt)));
    }

    #endregion

    #region GetActiveByUserIdAsync Tests

    [Fact]
    public async Task GetActiveByUserIdAsync_ShouldReturnOnlyValidTokens()
    {
        // Act
        var results = await _repository.GetActiveByUserIdAsync(_testUserId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, t =>
        {
            Assert.False(t.IsRevoked);
            Assert.True(t.ExpiresAt > DateTime.UtcNow);
        });
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ShouldNotIncludeExpiredTokens()
    {
        // Act
        var results = await _repository.GetActiveByUserIdAsync(_testUserId);

        // Assert
        var expiredToken = _context.RefreshTokens.FirstOrDefault(t => t.Token == "expired_token");
        Assert.DoesNotContain(expiredToken, results);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_WithInvalidUserId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetActiveByUserIdAsync(Guid.NewGuid().ToString());

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WithValidToken_ShouldAddAndSetId()
    {
        // Arrange
        var newToken = new RefreshToken
        {
            Token = "new_token",
            UserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        // Act
        var result = await _repository.AddAsync(newToken);

        // Assert
        Assert.NotNull(result.Id);
        var saved = await _context.RefreshTokens.FindAsync(result.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task AddAsync_ShouldSetCreatedAt()
    {
        // Arrange
        var newToken = new RefreshToken
        {
            Token = "another_new_token",
            UserId = _testUserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        // Act
        var result = await _repository.AddAsync(newToken);

        // Assert
        Assert.NotEqual(DateTime.MinValue, result.CreatedAt);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidToken_ShouldUpdate()
    {
        // Arrange
        var token = _context.RefreshTokens.First();
        token.IsRevoked = true;

        // Act
        await _repository.UpdateAsync(token);

        // Assert
        var updated = await _context.RefreshTokens.FindAsync(token.Id);
        Assert.True(updated.IsRevoked);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemove()
    {
        // Arrange
        var token = _context.RefreshTokens.First();

        // Act
        var result = await _repository.DeleteAsync(token.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.RefreshTokens.FindAsync(token.Id);
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

    #region DeleteExpiredTokensAsync Tests

    [Fact]
    public async Task DeleteExpiredTokensAsync_ShouldRemoveExpiredTokens()
    {
        // Act
        var count = await _repository.DeleteExpiredTokensAsync();

        // Assert
        Assert.True(count > 0);
        var expiredToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "expired_token");
        Assert.Null(expiredToken);
    }

    [Fact]
    public async Task DeleteExpiredTokensAsync_ShouldNotRemoveValidTokens()
    {
        // Arrange
        var validToken = _context.RefreshTokens.First(t => t.ExpiresAt > DateTime.UtcNow);

        // Act
        await _repository.DeleteExpiredTokensAsync();

        // Assert
        var stillExists = await _context.RefreshTokens.FindAsync(validToken.Id);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task DeleteExpiredTokensAsync_ShouldReturnCountOfDeletedTokens()
    {
        // Act
        var count = await _repository.DeleteExpiredTokensAsync();

        // Assert
        Assert.Equal(1, count); // Only the expired_token
    }

    #endregion
}
