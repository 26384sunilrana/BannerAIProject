namespace BannerService.Domain.Tests.Entities;

using System;
using System.Text.Json;
using Xunit;
using BannerService.Domain.Entities;
using BannerService.Domain.ValueObjects;

public class BannerVersionEntityTests
{
    private readonly Guid _testBannerId = Guid.NewGuid();
    private readonly Guid _testShopId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    private BannerSnapshot CreateTestSnapshot()
    {
        return new BannerSnapshot
        {
            BannerId = _testBannerId,
            Name = "Test Banner",
            Width = 1920,
            Height = 1080
        };
    }

    #region Construction Tests

    [Fact]
    public void CreateBannerVersion_WithValidData_ShouldInitializeProperties()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();

        // Act
        var version = new BannerVersion(
            _testBannerId,
            _testShopId,
            1,
            snapshot,
            _testUserId,
            "Initial version"
        );

        // Assert
        Assert.NotEqual(Guid.Empty, version.Id);
        Assert.Equal(_testBannerId, version.BannerId);
        Assert.Equal(_testShopId, version.ShopId);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(_testUserId, version.CreatedBy);
        Assert.Equal("Initial version", version.ChangeDescription);
        Assert.True(version.IsActive);
        Assert.NotEqual(DateTime.MinValue, version.CreatedAt);
    }

    [Fact]
    public void CreateBannerVersion_WithoutChangeDescription_ShouldAllowNull()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();

        // Act
        var version = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId);

        // Assert
        Assert.Null(version.ChangeDescription);
    }

    [Fact]
    public void CreateBannerVersion_WithNullSnapshot_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new BannerVersion(_testBannerId, _testShopId, 1, null, _testUserId)
        );
    }

    [Fact]
    public void CreateBannerVersion_WithLongChangeDescription_ShouldThrowException()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var longDescription = new string('a', 501); // 501 characters, exceeds 500 limit

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId, longDescription)
        );
    }

    [Fact]
    public void CreateBannerVersion_WithMaxLengthChangeDescription_ShouldSucceed()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var maxDescription = new string('a', 500); // Exactly 500 characters

        // Act
        var version = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId, maxDescription);

        // Assert
        Assert.NotNull(version.ChangeDescription);
        Assert.Equal(500, version.ChangeDescription.Length);
    }

    #endregion

    #region Snapshot Serialization Tests

    [Fact]
    public void Snapshot_ShouldSerializeToJson()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var version = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId);

        // Act
        var retrievedSnapshot = version.Snapshot;

        // Assert
        Assert.NotNull(retrievedSnapshot);
        Assert.Equal(snapshot.BannerId, retrievedSnapshot.BannerId);
        Assert.Equal(snapshot.Name, retrievedSnapshot.Name);
    }

    [Fact]
    public void Snapshot_WithEmptyJson_ShouldReturnDefaultSnapshot()
    {
        // Arrange
        var version = new BannerVersion();
        version.SnapshotJson = string.Empty;

        // Act
        var snapshot = version.Snapshot;

        // Assert
        Assert.NotNull(snapshot);
    }

    [Fact]
    public void SetSnapshot_ShouldSerializeToJson()
    {
        // Arrange
        var version = new BannerVersion
        {
            BannerId = _testBannerId,
            ShopId = _testShopId,
            VersionNumber = 1,
            CreatedBy = _testUserId
        };

        var newSnapshot = new BannerSnapshot
        {
            BannerId = _testBannerId,
            Name = "Updated Banner",
            Width = 2560,
            Height = 1440
        };

        // Act
        version.Snapshot = newSnapshot;

        // Assert
        Assert.NotEmpty(version.SnapshotJson);
        var deserialized = JsonSerializer.Deserialize<BannerSnapshot>(version.SnapshotJson);
        Assert.Equal("Updated Banner", deserialized?.Name);
    }

    #endregion

    #region Version Management Tests

    [Fact]
    public void VersionNumber_ShouldBePersisted()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();

        // Act
        var version1 = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId);
        var version2 = new BannerVersion(_testBannerId, _testShopId, 2, snapshot, _testUserId);

        // Assert
        Assert.Equal(1, version1.VersionNumber);
        Assert.Equal(2, version2.VersionNumber);
    }

    [Fact]
    public void MultipleVersions_CanExistForSameBanner()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var versions = new[]
        {
            new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId, "Version 1"),
            new BannerVersion(_testBannerId, _testShopId, 2, snapshot, _testUserId, "Version 2"),
            new BannerVersion(_testBannerId, _testShopId, 3, snapshot, _testUserId, "Version 3")
        };

        // Act & Assert
        Assert.All(versions, v => Assert.Equal(_testBannerId, v.BannerId));
        Assert.True(versions[0].VersionNumber < versions[1].VersionNumber);
        Assert.True(versions[1].VersionNumber < versions[2].VersionNumber);
    }

    #endregion

    #region Audit Trail Tests

    [Fact]
    public void CreatedBy_ShouldTrackUserResponsibleForVersion()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var userId = Guid.NewGuid();

        // Act
        var version = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, userId);

        // Assert
        Assert.Equal(userId, version.CreatedBy);
    }

    [Fact]
    public void CreatedAt_ShouldTrackVersionCreationTime()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var beforeCreation = DateTime.UtcNow;

        // Act
        var version = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId);

        // Assert
        Assert.True(version.CreatedAt >= beforeCreation);
        Assert.True(version.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void ChangeDescription_ShouldDocumentChanges()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var changeDesc = "Updated banner width to 2560px and height to 1440px";

        // Act
        var version = new BannerVersion(_testBannerId, _testShopId, 2, snapshot, _testUserId, changeDesc);

        // Assert
        Assert.Equal(changeDesc, version.ChangeDescription);
    }

    #endregion

    #region Active Status Tests

    [Fact]
    public void NewVersion_ShouldBeActive()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();

        // Act
        var version = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId);

        // Assert
        Assert.True(version.IsActive);
    }

    [Fact]
    public void IsActive_CanBeChanged()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var version = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId);

        // Act
        version.IsActive = false;

        // Assert
        Assert.False(version.IsActive);
    }

    #endregion

    #region Snapshot Comparison Tests

    [Fact]
    public void TwoVersions_CanHaveDifferentSnapshots()
    {
        // Arrange
        var snapshot1 = new BannerSnapshot
        {
            BannerId = _testBannerId,
            Name = "Version 1",
            Width = 1920,
            Height = 1080
        };

        var snapshot2 = new BannerSnapshot
        {
            BannerId = _testBannerId,
            Name = "Version 2",
            Width = 2560,
            Height = 1440
        };

        // Act
        var version1 = new BannerVersion(_testBannerId, _testShopId, 1, snapshot1, _testUserId);
        var version2 = new BannerVersion(_testBannerId, _testShopId, 2, snapshot2, _testUserId);

        // Assert
        Assert.NotEqual(version1.Snapshot.Name, version2.Snapshot.Name);
        Assert.NotEqual(version1.Snapshot.Width, version2.Snapshot.Width);
    }

    #endregion

    #region Shop Isolation Tests

    [Fact]
    public void Version_ShouldBelongToSpecificShop()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();
        var shopId = Guid.NewGuid();

        // Act
        var version = new BannerVersion(_testBannerId, shopId, 1, snapshot, _testUserId);

        // Assert
        Assert.Equal(shopId, version.ShopId);
    }

    [Fact]
    public void MultipleVersions_CanBelongToSameBannerAndShop()
    {
        // Arrange
        var snapshot = CreateTestSnapshot();

        // Act
        var v1 = new BannerVersion(_testBannerId, _testShopId, 1, snapshot, _testUserId);
        var v2 = new BannerVersion(_testBannerId, _testShopId, 2, snapshot, _testUserId);

        // Assert
        Assert.Equal(v1.ShopId, v2.ShopId);
        Assert.Equal(v1.BannerId, v2.BannerId);
        Assert.NotEqual(v1.VersionNumber, v2.VersionNumber);
    }

    #endregion
}
