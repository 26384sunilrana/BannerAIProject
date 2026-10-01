namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class MediaFileEntityTests
{
    #region Constructor Tests

    [Fact]
    public void CreateMediaFile_WithValidData_ShouldInitializeProperties()
    {
        // Act
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "image.jpg",
            FileSize = 1024000,
            FileType = "image/jpeg",
            UploadStatus = UploadStatus.Completed
        };

        // Assert
        Assert.NotEqual(Guid.Empty, mediaFile.Id);
        Assert.Equal("image.jpg", mediaFile.FileName);
        Assert.Equal(1024000, mediaFile.FileSize);
        Assert.Equal("image/jpeg", mediaFile.FileType);
        Assert.Equal(UploadStatus.Completed, mediaFile.UploadStatus);
    }

    #endregion

    #region UploadStatus Tests

    [Fact]
    public void UploadStatus_Pending_ShouldIndicateWaitingToUpload()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "pending.mp4",
            FileSize = 0,
            UploadStatus = UploadStatus.Pending
        };

        // Act & Assert
        Assert.Equal(UploadStatus.Pending, mediaFile.UploadStatus);
    }

    [Fact]
    public void UploadStatus_InProgress_ShouldIndicateUploading()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "uploading.mp4",
            FileSize = 5242880,
            UploadStatus = UploadStatus.InProgress
        };

        // Act & Assert
        Assert.Equal(UploadStatus.InProgress, mediaFile.UploadStatus);
    }

    [Fact]
    public void UploadStatus_Completed_ShouldIndicateSuccessful()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "completed.jpg",
            FileSize = 2048000,
            UploadStatus = UploadStatus.Completed
        };

        // Act & Assert
        Assert.Equal(UploadStatus.Completed, mediaFile.UploadStatus);
    }

    [Fact]
    public void UploadStatus_Failed_ShouldIndicateError()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "failed.zip",
            FileSize = 0,
            UploadStatus = UploadStatus.Failed,
            ErrorMessage = "File size exceeds limit"
        };

        // Act & Assert
        Assert.Equal(UploadStatus.Failed, mediaFile.UploadStatus);
        Assert.Equal("File size exceeds limit", mediaFile.ErrorMessage);
    }

    [Fact]
    public void UploadStatus_Processing_ShouldIndicatePostUpload()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "processing.mp4",
            FileSize = 10485760,
            UploadStatus = UploadStatus.Processing
        };

        // Act & Assert
        Assert.Equal(UploadStatus.Processing, mediaFile.UploadStatus);
    }

    #endregion

    #region File Type Tests

    [Fact]
    public void FileType_ShouldStoreMultimediaTypes()
    {
        // Arrange & Act
        var imageFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "photo.jpg",
            FileType = "image/jpeg",
            FileSize = 2048000
        };

        // Assert
        Assert.Equal("image/jpeg", imageFile.FileType);
    }

    [Fact]
    public void FileType_ShouldSupportVideos()
    {
        // Arrange & Act
        var videoFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "video.mp4",
            FileType = "video/mp4",
            FileSize = 10485760
        };

        // Assert
        Assert.Equal("video/mp4", videoFile.FileType);
    }

    [Fact]
    public void FileType_ShouldSupportDocuments()
    {
        // Arrange & Act
        var docFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "document.pdf",
            FileType = "application/pdf",
            FileSize = 1048576
        };

        // Assert
        Assert.Equal("application/pdf", docFile.FileType);
    }

    #endregion

    #region File Size Tests

    [Fact]
    public void FileSize_ShouldStoreBytes()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "large_file.zip",
            FileSize = 1073741824  // 1 GB
        };

        // Act & Assert
        Assert.Equal(1073741824, mediaFile.FileSize);
    }

    [Fact]
    public void FileSize_ShouldBeZeroForPendingUploads()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "pending.mov",
            FileSize = 0,
            UploadStatus = UploadStatus.Pending
        };

        // Act & Assert
        Assert.Equal(0, mediaFile.FileSize);
    }

    #endregion

    #region Storage Path Tests

    [Fact]
    public void StoragePath_ShouldBuildPath()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "banner.jpg",
            StoragePath = "/uploads/shops/shop123/banner.jpg"
        };

        // Act & Assert
        Assert.Equal("/uploads/shops/shop123/banner.jpg", mediaFile.StoragePath);
    }

    [Fact]
    public void StoragePath_ShouldBeOptional()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "temp.jpg",
            StoragePath = null
        };

        // Act & Assert
        Assert.Null(mediaFile.StoragePath);
    }

    #endregion

    #region Timestamps Tests

    [Fact]
    public void CreatedAt_ShouldBeSetOnCreation()
    {
        // Arrange
        var beforeTime = DateTime.UtcNow;

        // Act
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "timed.jpg",
            FileSize = 1024000,
            CreatedAt = DateTime.UtcNow
        };

        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.True(mediaFile.CreatedAt >= beforeTime);
        Assert.True(mediaFile.CreatedAt <= afterTime);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public void Metadata_ShouldBeOptional()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "plain.jpg",
            FileSize = 1024000,
            Metadata = null
        };

        // Act & Assert
        Assert.Null(mediaFile.Metadata);
    }

    [Fact]
    public void Metadata_ShouldStoreAdditionalInfo()
    {
        // Arrange
        var mediaFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            FileName = "detailed.jpg",
            FileSize = 2048000,
            Metadata = "{\"width\": 1920, \"height\": 1080, \"duration\": null}"
        };

        // Act & Assert
        Assert.NotNull(mediaFile.Metadata);
        Assert.Contains("width", mediaFile.Metadata);
    }

    #endregion
}
