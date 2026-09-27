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

public class MediaFileRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private MediaFileRepository _repository;
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _otherShopId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new MediaFileRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var mediaFile1 = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            FileName = "image1.jpg",
            FileSize = 1024000,
            FileType = "image/jpeg",
            UploadStatus = UploadStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        var mediaFile2 = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            FileName = "video1.mp4",
            FileSize = 5242880,
            FileType = "video/mp4",
            UploadStatus = UploadStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var otherShopFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = _otherShopId,
            FileName = "other.jpg",
            FileSize = 512000,
            FileType = "image/jpeg",
            UploadStatus = UploadStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };

        _context.MediaFiles.AddRange(mediaFile1, mediaFile2, otherShopFile);
        await _context.SaveChangesAsync();
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithValidMediaFile_ShouldAddToDatabase()
    {
        // Arrange
        var newFile = new MediaFile
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            FileName = "new_image.png",
            FileSize = 2048000,
            FileType = "image/png",
            UploadStatus = UploadStatus.Pending
        };

        // Act
        var result = await _repository.SaveAsync(newFile);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.MediaFiles.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("new_image.png", saved.FileName);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidIdAndShop_ShouldReturnMediaFile()
    {
        // Arrange
        var file = _context.MediaFiles.First(m => m.ShopId == _shopId);

        // Act
        var result = await _repository.GetByIdAsync(file.Id, _shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(file.Id, result.Id);
        Assert.Equal(file.FileName, result.FileName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), _shopId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentShop_ShouldReturnNull()
    {
        // Arrange
        var file = _context.MediaFiles.First(m => m.ShopId == _shopId);

        // Act
        var result = await _repository.GetByIdAsync(file.Id, _otherShopId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByShopAsync Tests

    [Fact]
    public async Task GetByShopAsync_WithValidShopId_ShouldReturnMediaFiles()
    {
        // Act
        var results = await _repository.GetByShopAsync(_shopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, m => Assert.Equal(_shopId, m.ShopId));
    }

    [Fact]
    public async Task GetByShopAsync_ShouldBeOrderedByCreatedAtDescending()
    {
        // Act
        var results = await _repository.GetByShopAsync(_shopId);

        // Assert
        if (results.Count > 1)
        {
            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.True(results[i].CreatedAt >= results[i + 1].CreatedAt);
            }
        }
    }

    [Fact]
    public async Task GetByShopAsync_WithInvalidShop_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByShopAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidMediaFile_ShouldUpdateDatabase()
    {
        // Arrange
        var file = _context.MediaFiles.First(m => m.ShopId == _shopId);
        file.UploadStatus = UploadStatus.Completed;

        // Act
        await _repository.UpdateAsync(file);

        // Assert
        var updated = await _context.MediaFiles.FindAsync(file.Id);
        Assert.Equal(UploadStatus.Completed, updated.UploadStatus);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidIdAndShop_ShouldRemoveFromDatabase()
    {
        // Arrange
        var file = _context.MediaFiles.First(m => m.ShopId == _shopId);

        // Act
        await _repository.DeleteAsync(file.Id, _shopId);

        // Assert
        var deleted = await _context.MediaFiles.FindAsync(file.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithDifferentShop_ShouldNotDelete()
    {
        // Arrange
        var file = _context.MediaFiles.First(m => m.ShopId == _shopId);

        // Act
        await _repository.DeleteAsync(file.Id, _otherShopId);

        // Assert
        var notDeleted = await _context.MediaFiles.FindAsync(file.Id);
        Assert.NotNull(notDeleted);
    }

    #endregion
}

public class UploadChunkRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private UploadChunkRepository _repository;
    private readonly Guid _mediaFileId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new UploadChunkRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var chunk1 = new UploadChunk
        {
            Id = Guid.NewGuid(),
            MediaFileId = _mediaFileId,
            ChunkNumber = 1,
            ChunkSize = 1048576,
            ChunkHash = "hash1"
        };

        var chunk2 = new UploadChunk
        {
            Id = Guid.NewGuid(),
            MediaFileId = _mediaFileId,
            ChunkNumber = 2,
            ChunkSize = 1048576,
            ChunkHash = "hash2"
        };

        _context.UploadChunks.AddRange(chunk1, chunk2);
        await _context.SaveChangesAsync();
    }

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_WithValidChunk_ShouldAddToDatabase()
    {
        // Arrange
        var newChunk = new UploadChunk
        {
            Id = Guid.NewGuid(),
            MediaFileId = Guid.NewGuid(),
            ChunkNumber = 1,
            ChunkSize = 1048576,
            ChunkHash = "newhash"
        };

        // Act
        var result = await _repository.SaveAsync(newChunk);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.UploadChunks.FindAsync(result.Id);
        Assert.NotNull(saved);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnChunk()
    {
        // Arrange
        var chunk = _context.UploadChunks.First();

        // Act
        var result = await _repository.GetByIdAsync(chunk.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(chunk.Id, result.Id);
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

    #region GetChunksByMediaFileAsync Tests

    [Fact]
    public async Task GetChunksByMediaFileAsync_WithValidMediaFileId_ShouldReturnChunks()
    {
        // Act
        var results = await _repository.GetChunksByMediaFileAsync(_mediaFileId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, c => Assert.Equal(_mediaFileId, c.MediaFileId));
    }

    [Fact]
    public async Task GetChunksByMediaFileAsync_ShouldBeOrderedByChunkNumber()
    {
        // Act
        var results = await _repository.GetChunksByMediaFileAsync(_mediaFileId);

        // Assert
        var numbers = results.Select(c => c.ChunkNumber).ToList();
        var sorted = numbers.OrderBy(n => n).ToList();
        Assert.Equal(sorted, numbers);
    }

    #endregion

    #region GetChunkAsync Tests

    [Fact]
    public async Task GetChunkAsync_WithValidMediaFileAndChunkNumber_ShouldReturnChunk()
    {
        // Act
        var result = await _repository.GetChunkAsync(_mediaFileId, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ChunkNumber);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidChunk_ShouldUpdateDatabase()
    {
        // Arrange
        var chunk = _context.UploadChunks.First();
        chunk.ChunkHash = "updatedhash";

        // Act
        await _repository.UpdateAsync(chunk);

        // Assert
        var updated = await _context.UploadChunks.FindAsync(chunk.Id);
        Assert.Equal("updatedhash", updated.ChunkHash);
    }

    #endregion

    #region DeleteChunkAsync Tests

    [Fact]
    public async Task DeleteChunkAsync_WithValidId_ShouldRemoveFromDatabase()
    {
        // Arrange
        var chunk = _context.UploadChunks.First();

        // Act
        await _repository.DeleteChunkAsync(chunk.Id);

        // Assert
        var deleted = await _context.UploadChunks.FindAsync(chunk.Id);
        Assert.Null(deleted);
    }

    #endregion

    #region GetChunkCountAsync Tests

    [Fact]
    public async Task GetChunkCountAsync_ShouldReturnCorrectCount()
    {
        // Act
        var count = await _repository.GetChunkCountAsync(_mediaFileId);

        // Assert
        Assert.Equal(2, count);
    }

    #endregion
}
