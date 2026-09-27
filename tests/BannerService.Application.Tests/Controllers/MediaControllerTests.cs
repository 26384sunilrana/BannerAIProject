namespace BannerService.Application.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BannerService.Presentation.Controllers;
using BannerService.Application.Dto;
using BannerService.Application.Services;

public class MediaControllerTests
{
    private readonly Mock<IMediaUploadService> _mockMediaUploadService;
    private readonly Mock<ILogger<MediaController>> _mockLogger;
    private readonly MediaController _controller;
    private readonly Guid _testShopId;
    private readonly Guid _testUserId;
    private readonly Guid _testMediaFileId;

    public MediaControllerTests()
    {
        _mockMediaUploadService = new Mock<IMediaUploadService>();
        _mockLogger = new Mock<ILogger<MediaController>>();
        _testShopId = Guid.NewGuid();
        _testUserId = Guid.NewGuid();
        _testMediaFileId = Guid.NewGuid();

        _controller = new MediaController(_mockMediaUploadService.Object, _mockLogger.Object);

        // Setup controller context with claims
        var claims = new List<Claim>
        {
            new Claim("shop_id", _testShopId.ToString()),
            new Claim("sub", _testUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region InitializeUpload Tests

    [Fact]
    public async Task InitializeUpload_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new InitializeUploadRequestDto
        {
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            TotalSizeBytes = 1024000,
            FileType = 1
        };

        var mediaFile = new MediaFileDto
        {
            Id = _testMediaFileId,
            FileName = request.FileName,
            SizeBytes = request.TotalSizeBytes
        };

        _mockMediaUploadService.Setup(s => s.InitializeUploadAsync(
            request.FileName, request.ContentType, request.TotalSizeBytes,
            request.FileType, _testShopId, _testUserId))
            .ReturnsAsync(mediaFile);

        // Act
        var result = await _controller.InitializeUpload(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task InitializeUpload_WithMissingFileName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new InitializeUploadRequestDto
        {
            FileName = "",
            ContentType = "image/jpeg",
            TotalSizeBytes = 1024000,
            FileType = 1
        };

        // Act
        var result = await _controller.InitializeUpload(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task InitializeUpload_WithInvalidFileType_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new InitializeUploadRequestDto
        {
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            TotalSizeBytes = 1024000,
            FileType = 10
        };

        // Act
        var result = await _controller.InitializeUpload(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region UploadChunk Tests

    [Fact]
    public async Task UploadChunk_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var chunkData = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = chunkData;
        httpContext.Request.Headers["X-Checksum-MD5"] = "abc123def456";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var response = new ChunkUploadResponseDto { ChunkNumber = 1, IsLastChunk = false };

        _mockMediaUploadService.Setup(s => s.UploadChunkAsync(
            _testMediaFileId, 1, It.IsAny<MemoryStream>(), "abc123def456", _testShopId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UploadChunk(_testMediaFileId, 1, "abc123def456");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task UploadChunk_WithEmptyMediaFileId_ShouldReturnBadRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.UploadChunk(Guid.Empty, 1, "abc123def456");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UploadChunk_WithMissingChecksum_ShouldReturnBadRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.UploadChunk(_testMediaFileId, 1, "");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region CompleteUpload Tests

    [Fact]
    public async Task CompleteUpload_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var mediaFile = new MediaFileDto
        {
            Id = _testMediaFileId,
            FileName = "test.jpg",
            SizeBytes = 1024000
        };

        _mockMediaUploadService.Setup(s => s.CompleteUploadAsync(_testMediaFileId, _testShopId))
            .ReturnsAsync(mediaFile);

        // Act
        var result = await _controller.CompleteUpload(_testMediaFileId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CompleteUpload_WithInvalidId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var invalidMediaFileId = Guid.NewGuid();
        _mockMediaUploadService.Setup(s => s.CompleteUploadAsync(invalidMediaFileId, _testShopId))
            .ThrowsAsync(new KeyNotFoundException("Media file not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _controller.CompleteUpload(invalidMediaFileId)
        );
    }

    #endregion

    #region GetMediaFile Tests

    [Fact]
    public async Task GetMediaFile_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var mediaFile = new MediaFileDto
        {
            Id = _testMediaFileId,
            FileName = "test.jpg",
            SizeBytes = 1024000
        };

        _mockMediaUploadService.Setup(s => s.GetMediaFileAsync(_testMediaFileId, _testShopId))
            .ReturnsAsync(mediaFile);

        // Act
        var result = await _controller.GetMediaFile(_testMediaFileId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMediaFile_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidMediaFileId = Guid.NewGuid();
        _mockMediaUploadService.Setup(s => s.GetMediaFileAsync(invalidMediaFileId, _testShopId))
            .ReturnsAsync((MediaFileDto)null);

        // Act
        var result = await _controller.GetMediaFile(invalidMediaFileId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    #endregion

    #region GetMediaUrl Tests

    [Fact]
    public async Task GetMediaUrl_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var mediaUrl = new MediaUrlDto { Url = "https://example.com/media/test.jpg" };

        _mockMediaUploadService.Setup(s => s.GetMediaUrlAsync(_testMediaFileId, _testShopId, null))
            .ReturnsAsync(mediaUrl);

        // Act
        var result = await _controller.GetMediaUrl(_testMediaFileId, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}
