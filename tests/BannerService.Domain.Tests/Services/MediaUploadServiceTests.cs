namespace BannerService.Domain.Tests.Services;

using Xunit;
using Moq;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Application.Services;

public class MediaUploadServiceTests
{
    private readonly Mock<IMediaFileRepository> _mockMediaFileRepository = new();
    private readonly Mock<IUploadChunkRepository> _mockChunkRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly MediaUploadService _service;

    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public MediaUploadServiceTests()
    {
        _service = new MediaUploadService(
            _mockMediaFileRepository.Object,
            _mockChunkRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task InitializeUploadAsync_WithValidParams_ShouldCreateMediaFile()
    {
        // Arrange
        var fileName = "video.mp4";
        var contentType = "video/mp4";
        var totalSize = 104857600;  // 100MB
        var fileType = 2;  // Video

        _mockMediaFileRepository.Setup(r => r.SaveAsync(It.IsAny<MediaFile>()))
            .ReturnsAsync((MediaFile m) => m);
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.InitializeUploadAsync(fileName, contentType, totalSize, fileType, _shopId, _userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(totalSize, result.SizeBytes);
        Assert.Equal(1, result.Status);  // Pending
    }

    [Fact]
    public async Task InitializeUploadAsync_WithFileTooLarge_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.InitializeUploadAsync("big.mp4", "video/mp4", 536870913, 2, _shopId, _userId));
    }

    [Fact]
    public async Task InitializeUploadAsync_WithInvalidFileType_ShouldThrow()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.InitializeUploadAsync("file.txt", "text/plain", 1024, 5, _shopId, _userId));
    }

    [Fact]
    public async Task UploadChunkAsync_WithValidData_ShouldCreateChunk()
    {
        // Arrange
        var mediaFile = new MediaFile(_shopId, "video.mp4", "video/mp4", 104857600, 2, _userId);
        var chunkData = new byte[1024 * 1024];  // 1MB
        new Random().NextBytes(chunkData);

        using var ms = new MemoryStream(chunkData);
        using var sha = System.Security.Cryptography.MD5.Create();
        var checksum = BitConverter.ToString(sha.ComputeHash(chunkData)).Replace("-", "").ToLowerInvariant();

        _mockMediaFileRepository.Setup(r => r.GetByIdAsync(mediaFile.Id, _shopId))
            .ReturnsAsync(mediaFile);
        _mockChunkRepository.Setup(r => r.GetChunkAsync(mediaFile.Id, 0))
            .ReturnsAsync((UploadChunk?)null);
        _mockChunkRepository.Setup(r => r.SaveAsync(It.IsAny<UploadChunk>()))
            .ReturnsAsync((UploadChunk c) => c);
        _mockChunkRepository.Setup(r => r.UpdateAsync(It.IsAny<UploadChunk>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadChunkAsync(mediaFile.Id, 0, ms, checksum, _shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.ChunkNumber);
        Assert.Equal(2, result.Status);  // Uploaded
    }

    [Fact]
    public async Task UploadChunkAsync_WithChecksumMismatch_ShouldThrow()
    {
        // Arrange
        var mediaFile = new MediaFile(_shopId, "video.mp4", "video/mp4", 104857600, 2, _userId);
        var chunkData = new byte[1024];
        new Random().NextBytes(chunkData);

        using var ms = new MemoryStream(chunkData);
        var wrongChecksum = "00000000000000000000000000000000";

        _mockMediaFileRepository.Setup(r => r.GetByIdAsync(mediaFile.Id, _shopId))
            .ReturnsAsync(mediaFile);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UploadChunkAsync(mediaFile.Id, 0, ms, wrongChecksum, _shopId));
    }

    [Fact]
    public async Task CompleteUploadAsync_WithAllChunksUploaded_ShouldMarkAsActive()
    {
        // Arrange
        var mediaFile = new MediaFile(_shopId, "video.mp4", "video/mp4", 104857600, 2, _userId);
        var chunk1 = new UploadChunk(mediaFile.Id, 0, 104857600, "abc123abc123abc123abc123abc12345");
        chunk1.MarkAsUploaded();

        var chunks = new List<UploadChunk> { chunk1 };

        _mockMediaFileRepository.Setup(r => r.GetByIdAsync(mediaFile.Id, _shopId))
            .ReturnsAsync(mediaFile);
        _mockChunkRepository.Setup(r => r.GetChunksByMediaFileAsync(mediaFile.Id))
            .ReturnsAsync(chunks);
        _mockChunkRepository.Setup(r => r.UpdateAsync(It.IsAny<UploadChunk>()))
            .Returns(Task.CompletedTask);
        _mockMediaFileRepository.Setup(r => r.UpdateAsync(It.IsAny<MediaFile>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CompleteUploadAsync(mediaFile.Id, _shopId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Status);  // Active
    }

    [Fact]
    public async Task CompleteUploadAsync_WithMissingChunks_ShouldThrow()
    {
        // Arrange
        var mediaFile = new MediaFile(_shopId, "video.mp4", "video/mp4", 104857600, 2, _userId);

        _mockMediaFileRepository.Setup(r => r.GetByIdAsync(mediaFile.Id, _shopId))
            .ReturnsAsync(mediaFile);
        _mockChunkRepository.Setup(r => r.GetChunksByMediaFileAsync(mediaFile.Id))
            .ReturnsAsync(new List<UploadChunk>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CompleteUploadAsync(mediaFile.Id, _shopId));
    }

    [Fact]
    public async Task GetMediaUrlAsync_WithActiveFile_ShouldGenerateUrl()
    {
        // Arrange
        var mediaFile = new MediaFile(_shopId, "video.mp4", "video/mp4", 104857600, 2, _userId);
        mediaFile.MarkAsActive();

        _mockMediaFileRepository.Setup(r => r.GetByIdAsync(mediaFile.Id, _shopId))
            .ReturnsAsync(mediaFile);

        // Act
        var result = await _service.GetMediaUrlAsync(mediaFile.Id, _shopId, 60);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaFile.Id, result.MediaFileId);
        Assert.NotNull(result.Url);
        Assert.NotNull(result.ExpiresAt);
    }

    [Fact]
    public async Task GetMediaUrlAsync_WithPendingFile_ShouldThrow()
    {
        // Arrange
        var mediaFile = new MediaFile(_shopId, "video.mp4", "video/mp4", 104857600, 2, _userId);

        _mockMediaFileRepository.Setup(r => r.GetByIdAsync(mediaFile.Id, _shopId))
            .ReturnsAsync(mediaFile);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetMediaUrlAsync(mediaFile.Id, _shopId));
    }

    [Fact]
    public async Task GetMediaUrlAsync_WithExcessiveExpiration_ShouldThrow()
    {
        // Arrange
        var mediaFile = new MediaFile(_shopId, "video.mp4", "video/mp4", 104857600, 2, _userId);
        mediaFile.MarkAsActive();

        _mockMediaFileRepository.Setup(r => r.GetByIdAsync(mediaFile.Id, _shopId))
            .ReturnsAsync(mediaFile);

        // Act & Assert - more than 30 days is rejected
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetMediaUrlAsync(mediaFile.Id, _shopId, 43200 + 10000));
    }
}
