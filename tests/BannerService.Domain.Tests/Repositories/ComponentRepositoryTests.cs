namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class ComponentRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private ComponentRepository _repository;
    private readonly Guid _bannerId = Guid.NewGuid();
    private readonly Guid _otherBannerId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new ComponentRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var component1 = new Component(
            _bannerId,
            ComponentType.Text,
            new Position(0, 0),
            new Size(100, 50),
            1,
            "{\"text\": \"Hello World\"}"
        );

        var component2 = new Component(
            _bannerId,
            ComponentType.Image,
            new Position(10, 10),
            new Size(200, 200),
            2,
            "{\"imageUrl\": \"https://example.com/image.jpg\"}"
        );

        var component3 = new Component(
            _otherBannerId,
            ComponentType.Video,
            new Position(0, 0),
            new Size(300, 300),
            1,
            "{\"videoUrl\": \"https://example.com/video.mp4\"}"
        );

        _context.Components.AddRange(component1, component2, component3);
        await _context.SaveChangesAsync();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidComponent_ShouldAddToDatabase()
    {
        // Arrange
        var newComponent = new Component(
            Guid.NewGuid(),
            ComponentType.Graphics,
            new Position(50, 50),
            new Size(150, 150),
            1,
            "{\"shape\": \"circle\"}"
        );

        // Act
        var result = await _repository.CreateAsync(newComponent);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.Components.FindAsync(result.Id);
        Assert.NotNull(saved);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnComponent()
    {
        // Arrange
        var component = _context.Components.First();

        // Act
        var result = await _repository.GetByIdAsync(component.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(component.Id, result.Id);
        Assert.Equal(component.ZIndex, result.ZIndex);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByBannerIdAsync Tests

    [Fact]
    public async Task GetByBannerIdAsync_WithValidBannerId_ShouldReturnComponents()
    {
        // Act
        var results = await _repository.GetByBannerIdAsync(_bannerId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, c => Assert.Equal(_bannerId, c.BannerId));
    }

    [Fact]
    public async Task GetByBannerIdAsync_ShouldBeOrderedByZIndex()
    {
        // Act
        var results = await _repository.GetByBannerIdAsync(_bannerId);

        // Assert
        if (results.Count > 1)
        {
            var zIndices = results.Select(c => c.ZIndex).ToList();
            var sortedZIndices = zIndices.OrderBy(z => z).ToList();
            Assert.Equal(sortedZIndices, zIndices);
        }
    }

    [Fact]
    public async Task GetByBannerIdAsync_WithInvalidBannerId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByBannerIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidComponent_ShouldUpdateDatabase()
    {
        // Arrange
        var component = _context.Components.First();
        var newPosition = new Position(100, 100);
        component.Update(newPosition, new Size(300, 300), 1, "{\"updated\": true}");

        // Act
        await _repository.UpdateAsync(component);

        // Assert
        var updated = await _context.Components.FindAsync(component.Id);
        Assert.Equal(100, updated.PositionX);
        Assert.Equal(100, updated.PositionY);
        Assert.Equal(300, updated.SizeWidth);
        Assert.Equal(300, updated.SizeHeight);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemoveFromDatabase()
    {
        // Arrange
        var component = _context.Components.First();

        // Act
        var result = await _repository.DeleteAsync(component.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Components.FindAsync(component.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ExistsByZIndexAsync Tests

    [Fact]
    public async Task ExistsByZIndexAsync_WithExistingZIndex_ShouldReturnTrue()
    {
        // Arrange
        var component = _context.Components.First(c => c.BannerId == _bannerId);

        // Act
        var result = await _repository.ExistsByZIndexAsync(_bannerId, component.ZIndex);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsByZIndexAsync_WithNonexistentZIndex_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsByZIndexAsync(_bannerId, 999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsByZIndexAsync_WithDifferentBanner_ShouldReturnFalse()
    {
        // Arrange
        var component = _context.Components.First(c => c.BannerId == _bannerId);

        // Act
        var result = await _repository.ExistsByZIndexAsync(_otherBannerId, component.ZIndex);

        // Assert
        Assert.False(result);
    }

    #endregion
}
