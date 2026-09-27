namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class AdvertisementEntityTests
{
    private readonly Guid _testShopId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    #region Construction Tests

    [Fact]
    public void CreateAdvertisement_WithValidData_ShouldInitializeProperties()
    {
        // Act
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Title = "Test Ad",
            Description = "Test Description",
            Type = AdType.Banner,
            Status = AdStatus.Draft
        };

        // Assert
        Assert.NotEqual(Guid.Empty, ad.Id);
        Assert.Equal(_testShopId, ad.ShopId);
        Assert.Equal(_testUserId, ad.CreatedByUserId);
        Assert.Equal("Test Ad", ad.Title);
        Assert.Equal("Test Description", ad.Description);
        Assert.Equal(AdType.Banner, ad.Type);
        Assert.Equal(AdStatus.Draft, ad.Status);
        Assert.False(ad.IsActive);
        Assert.Null(ad.PublishedAt);
    }

    [Fact]
    public void CreateAdvertisement_ShouldHaveDefaultMetrics()
    {
        // Act
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Title = "Test",
            Description = "Test"
        };

        // Assert
        Assert.NotNull(ad.Metrics);
    }

    [Fact]
    public void CreateAdvertisement_ShouldHaveCreatedTimestamp()
    {
        // Act
        var ad = new Advertisement { ShopId = _testShopId, CreatedByUserId = _testUserId };

        // Assert
        Assert.NotEqual(DateTime.MinValue, ad.CreatedAt);
        Assert.NotEqual(DateTime.MinValue, ad.UpdatedAt);
    }

    #endregion

    #region Activate Tests

    [Fact]
    public void Activate_FromDraftStatus_ShouldSetActiveAndPublished()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Draft
        };

        // Act
        ad.Activate(_testUserId);

        // Assert
        Assert.Equal(AdStatus.Active, ad.Status);
        Assert.True(ad.IsActive);
        Assert.NotNull(ad.PublishedAt);
    }

    [Fact]
    public void Activate_FromPausedStatus_ShouldSetActive()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Paused,
            IsActive = false
        };

        // Act
        ad.Activate(_testUserId);

        // Assert
        Assert.Equal(AdStatus.Active, ad.Status);
        Assert.True(ad.IsActive);
    }

    [Fact]
    public void Activate_FromInvalidStatus_ShouldThrowException()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Completed
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ad.Activate(_testUserId));
    }

    [Fact]
    public void Activate_ShouldUpdateModificationTime()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Draft
        };
        var originalUpdateTime = ad.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        ad.Activate(_testUserId);

        // Assert
        Assert.True(ad.UpdatedAt > originalUpdateTime);
    }

    #endregion

    #region Pause Tests

    [Fact]
    public void Pause_FromActiveStatus_ShouldSetPaused()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Active,
            IsActive = true
        };

        // Act
        ad.Pause(_testUserId);

        // Assert
        Assert.Equal(AdStatus.Paused, ad.Status);
        Assert.False(ad.IsActive);
        Assert.NotNull(ad.PausedAt);
    }

    [Fact]
    public void Pause_FromNonActiveStatus_ShouldThrowException()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Draft
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ad.Pause(_testUserId));
    }

    #endregion

    #region Complete Tests

    [Fact]
    public void Complete_FromActiveStatus_ShouldSetCompleted()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Active,
            IsActive = true
        };

        // Act
        ad.Complete(_testUserId);

        // Assert
        Assert.Equal(AdStatus.Completed, ad.Status);
        Assert.False(ad.IsActive);
        Assert.NotNull(ad.CompletedAt);
    }

    [Fact]
    public void Complete_FromPausedStatus_ShouldSetCompleted()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Paused,
            IsActive = false
        };

        // Act
        ad.Complete(_testUserId);

        // Assert
        Assert.Equal(AdStatus.Completed, ad.Status);
    }

    [Fact]
    public void Complete_FromInvalidStatus_ShouldThrowException()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Draft
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ad.Complete(_testUserId));
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_FromDraftStatus_ShouldSetCancelled()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Draft,
            IsActive = false
        };

        // Act
        ad.Cancel(_testUserId);

        // Assert
        Assert.Equal(AdStatus.Cancelled, ad.Status);
        Assert.False(ad.IsActive);
    }

    [Fact]
    public void Cancel_FromCompletedStatus_ShouldThrowException()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Completed
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ad.Cancel(_testUserId));
    }

    [Fact]
    public void Cancel_FromCancelledStatus_ShouldThrowException()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Cancelled
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => ad.Cancel(_testUserId));
    }

    #endregion

    #region Archive Tests

    [Fact]
    public void Archive_ShouldSetArchivedStatus()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Draft,
            IsActive = false
        };

        // Act
        ad.Archive(_testUserId);

        // Assert
        Assert.Equal(AdStatus.Archived, ad.Status);
        Assert.False(ad.IsActive);
    }

    #endregion

    #region Budget Tests

    [Fact]
    public void IsBudgetExceeded_WithExceededBudget_ShouldReturnTrue()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            BudgetLimit = 1000m
        };
        ad.Metrics.TotalSpent = 1500m;

        // Act
        var result = ad.IsBudgetExceeded();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBudgetExceeded_WithinBudget_ShouldReturnFalse()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            BudgetLimit = 1000m
        };
        ad.Metrics.TotalSpent = 500m;

        // Act
        var result = ad.IsBudgetExceeded();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsBudgetExceeded_WithZeroBudget_ShouldReturnFalse()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            BudgetLimit = 0m
        };

        // Act
        var result = ad.IsBudgetExceeded();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Expiration Tests

    [Fact]
    public void IsExpired_WithPastEndDate_ShouldReturnTrue()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            EndDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = ad.IsExpired();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpired_WithFutureEndDate_ShouldReturnFalse()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            EndDate = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var result = ad.IsExpired();

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Capability Tests

    [Fact]
    public void CanBeEdited_InDraftStatus_ShouldReturnTrue()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Draft
        };

        // Act
        var result = ad.CanBeEdited();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBeEdited_InNonDraftStatus_ShouldReturnFalse()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Active
        };

        // Act
        var result = ad.CanBeEdited();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBePaused_WhenActive_ShouldReturnTrue()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Active
        };

        // Act
        var result = ad.CanBePaused();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBeActivated_InDraftStatus_ShouldReturnTrue()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Draft
        };

        // Act
        var result = ad.CanBeActivated();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBeActivated_InPausedStatus_ShouldReturnTrue()
    {
        // Arrange
        var ad = new Advertisement
        {
            ShopId = _testShopId,
            CreatedByUserId = _testUserId,
            Status = AdStatus.Paused
        };

        // Act
        var result = ad.CanBeActivated();

        // Assert
        Assert.True(result);
    }

    #endregion
}
