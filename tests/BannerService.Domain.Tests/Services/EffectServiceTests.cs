namespace BannerService.Domain.Tests.Services;

using Xunit;
using Moq;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using BannerService.Domain.ValueObjects;
using BannerService.Application.Services;

public class EffectServiceTests
{
    private readonly Mock<IBannerRepository> _mockBannerRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly EffectValidator _validator = new();
    private readonly EffectService _service;

    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public EffectServiceTests()
    {
        _service = new EffectService(_mockBannerRepository.Object, _validator, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task ApplyEffectAsync_WithValidOpacity_ShouldCreateEffect()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        banner.AddComponent(component);

        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);
        _mockBannerRepository.Setup(r => r.UpdateAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);

        var parameters = new Dictionary<string, object>
        {
            { "opacity", 0.5m },
            { "duration", 1000 },
            { "delay", 0 }
        };

        // Act
        var result = await _service.ApplyEffectAsync(banner.Id, component.Id, _shopId, 1, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.EffectType);
        Assert.True(result.IsEnabled);
        Assert.Equal(3, result.Parameters.Count);
    }

    [Fact]
    public async Task ApplyEffectAsync_WithInvalidOpacity_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        banner.AddComponent(component);

        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);

        var parameters = new Dictionary<string, object>
        {
            { "opacity", 1.5m },  // Invalid
            { "duration", 1000 },
            { "delay", 0 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ApplyEffectAsync(banner.Id, component.Id, _shopId, 1, parameters));
    }

    [Fact]
    public async Task RemoveEffectAsync_WithValidEffect_ShouldRemove()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        var effect = new Effect(1, new Dictionary<string, object> { { "opacity", 0.5m }, { "duration", 1000 }, { "delay", 0 } });
        component.AddEffect(effect);
        banner.AddComponent(component);

        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);
        _mockBannerRepository.Setup(r => r.UpdateAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _service.RemoveEffectAsync(banner.Id, component.Id, effect.Id, _shopId);

        // Assert
        Assert.Empty(component.Effects);
        _mockBannerRepository.Verify(r => r.UpdateAsync(It.IsAny<Banner>()), Times.Once);
    }

    [Fact]
    public async Task GetComponentEffectsAsync_WithMultipleEffects_ShouldReturnAll()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");

        var effect1 = new Effect(1, new Dictionary<string, object> { { "opacity", 0.5m }, { "duration", 1000 }, { "delay", 0 } });
        var effect2 = new Effect(2, new Dictionary<string, object> { { "rotation", 45m }, { "duration", 2000 }, { "delay", 500 }, { "timingCurve", "ease-in" } });

        component.AddEffect(effect1);
        component.AddEffect(effect2);
        banner.AddComponent(component);

        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);

        // Act
        var results = await _service.GetComponentEffectsAsync(banner.Id, component.Id, _shopId);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, e => e.EffectType == 1);
        Assert.Contains(results, e => e.EffectType == 2);
    }

    [Fact]
    public async Task UpdateEffectAsync_WithValidNewParameters_ShouldCreateNewEffect()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        var effect = new Effect(1, new Dictionary<string, object> { { "opacity", 0.5m }, { "duration", 1000 }, { "delay", 0 } });
        component.AddEffect(effect);
        banner.AddComponent(component);

        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);
        _mockBannerRepository.Setup(r => r.UpdateAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);

        var newParameters = new Dictionary<string, object>
        {
            { "opacity", 0.8m },
            { "duration", 2000 },
            { "delay", 0 }
        };

        // Act
        var result = await _service.UpdateEffectAsync(banner.Id, component.Id, effect.Id, newParameters, _shopId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(effect.Id, result.Id);  // New effect created
        Assert.Single(component.Effects);  // Old effect removed, new one added
        Assert.Equal(0.8m, result.Parameters["opacity"]);
    }

    [Fact]
    public async Task ApplyEffectAsync_WithNonExistentComponent_ShouldThrow()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);

        var parameters = new Dictionary<string, object>
        {
            { "opacity", 0.5m },
            { "duration", 1000 },
            { "delay", 0 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ApplyEffectAsync(banner.Id, Guid.NewGuid(), _shopId, 1, parameters));
    }

    [Fact]
    public async Task ApplyEffectAsync_WithValidRotation_ShouldCreateEffect()
    {
        // Arrange
        var banner = new Banner(_shopId, _userId, "Test", "Desc", 1200, 600);
        var component = new Component(banner.Id, ComponentType.Text, new Position(0, 0), new Size(100, 50), 1, "{}");
        banner.AddComponent(component);

        _mockBannerRepository.Setup(r => r.GetByIdAsync(banner.Id, _shopId))
            .ReturnsAsync(banner);
        _mockBannerRepository.Setup(r => r.UpdateAsync(It.IsAny<Banner>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync())
            .Returns(Task.CompletedTask);

        var parameters = new Dictionary<string, object>
        {
            { "rotation", 45m },
            { "duration", 2000 },
            { "delay", 500 },
            { "timingCurve", "ease-in" }
        };

        // Act
        var result = await _service.ApplyEffectAsync(banner.Id, component.Id, _shopId, 2, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.EffectType);
        Assert.Equal(45m, result.Parameters["rotation"]);
    }
}
