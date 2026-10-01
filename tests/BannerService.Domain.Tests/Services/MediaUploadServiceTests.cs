namespace BannerService.Domain.Tests.Services;

using System.Security.Cryptography;
using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Application.Services;
using BannerService.Infrastructure.Security;
using BannerService.Infrastructure.Storage;

/// <summary>Keeps stored files in memory so uploads can be followed from the first chunk to the download.</summary>
public class InMemoryStorage : IStorageProvider
{
    public Dictionary<string, byte[]> Files { get; } = new();

    public Task SaveChunkAsync(string path, Stream data)
    {
        using var ms = new MemoryStream();
        data.CopyTo(ms);
        Files[path] = ms.ToArray();
        return Task.CompletedTask;
    }

    public Task AppendAsync(string path, Stream data)
    {
        using var ms = new MemoryStream();
        data.CopyTo(ms);
        Files[path] = Files.TryGetValue(path, out var existing) ? existing.Concat(ms.ToArray()).ToArray() : ms.ToArray();
        return Task.CompletedTask;
    }

    public Task<Stream> GetChunkAsync(string path) => Task.FromResult<Stream>(new MemoryStream(Files[path]));
    public Task<Stream> OpenReadAsync(string path) =>
        Files.TryGetValue(path, out var bytes) ? Task.FromResult<Stream>(new MemoryStream(bytes)) : throw new FileNotFoundException(path);
    public Task<bool> ChunkExistsAsync(string path) => Task.FromResult(Files.ContainsKey(path));
    public Task DeleteChunkAsync(string path) { Files.Remove(path); return Task.CompletedTask; }
    public Task<byte[]> GetFileAsync(string path) => Task.FromResult(Files[path]);
}

public class MediaUploadServiceTests
{
    private readonly Mock<IMediaFileRepository> _mockMediaFileRepository = new();
    private readonly Mock<IUploadChunkRepository> _mockChunkRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly InMemoryStorage _storage = new();
    private readonly MediaUrlSigner _signer;
    private readonly MediaUploadService _service;

    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly List<UploadChunk> _chunks = new();

    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    public MediaUploadServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Media:SigningKey"] = "test-signing-key-for-media-links-123456" })
            .Build();
        _signer = new MediaUrlSigner(configuration);

        _mockMediaFileRepository.Setup(r => r.SaveAsync(It.IsAny<MediaFile>())).ReturnsAsync((MediaFile m) => m);
        _mockMediaFileRepository.Setup(r => r.UpdateAsync(It.IsAny<MediaFile>())).Returns(Task.CompletedTask);
        _mockChunkRepository.Setup(r => r.SaveAsync(It.IsAny<UploadChunk>()))
            .ReturnsAsync((UploadChunk c) => { _chunks.Add(c); return c; });
        _mockChunkRepository.Setup(r => r.UpdateAsync(It.IsAny<UploadChunk>())).Returns(Task.CompletedTask);
        _mockChunkRepository.Setup(r => r.DeleteChunkAsync(It.IsAny<Guid>()))
            .Returns((Guid id) => { _chunks.RemoveAll(c => c.Id == id); return Task.CompletedTask; });
        _mockChunkRepository.Setup(r => r.GetChunkAsync(It.IsAny<Guid>(), It.IsAny<int>()))
            .ReturnsAsync((Guid _, int n) => _chunks.FirstOrDefault(c => c.ChunkNumber == n));
        _mockChunkRepository.Setup(r => r.GetChunksByMediaFileAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => _chunks.ToList());
        _mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);

        _service = new MediaUploadService(
            _mockMediaFileRepository.Object,
            _mockChunkRepository.Object,
            _mockUnitOfWork.Object,
            _storage,
            _signer);
    }

    private static string Md5(byte[] bytes) => BitConverter.ToString(MD5.HashData(bytes)).Replace("-", "").ToLowerInvariant();

    private MediaFile PendingFile(string contentType, long size, int fileType)
    {
        var file = new MediaFile(_shopId, "file.bin", contentType, size, fileType, _userId);
        _mockMediaFileRepository.Setup(r => r.GetByIdAsync(file.Id, _shopId)).ReturnsAsync(file);
        _mockMediaFileRepository.Setup(r => r.GetByIdUnscopedAsync(file.Id)).ReturnsAsync(file);
        return file;
    }

    private Task UploadAsync(MediaFile file, int chunkNumber, byte[] bytes) =>
        _service.UploadChunkAsync(file.Id, chunkNumber, new MemoryStream(bytes), Md5(bytes), _shopId);

    private static byte[] Png(int totalLength)
    {
        var bytes = new byte[totalLength];
        PngHeader.CopyTo(bytes, 0);
        return bytes;
    }

    #region Initialize

    [Fact]
    public async Task InitializeUploadAsync_WithValidParams_ShouldCreateMediaFile()
    {
        var result = await _service.InitializeUploadAsync("video.mp4", "video/mp4", 104857600, 2, _shopId, _userId);

        Assert.Equal("video.mp4", result.FileName);
        Assert.Equal("video/mp4", result.ContentType);
        Assert.Equal(104857600, result.SizeBytes);
        Assert.Equal(1, result.Status);  // Pending
    }

    [Fact]
    public async Task InitializeUploadAsync_WithFileTooLarge_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.InitializeUploadAsync("big.mp4", "video/mp4", 536870913, 2, _shopId, _userId));
    }

    [Fact]
    public async Task InitializeUploadAsync_WithInvalidFileType_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.InitializeUploadAsync("file.txt", "text/plain", 1024, 5, _shopId, _userId));
    }

    [Theory]
    [InlineData("image/svg+xml", 1)]
    [InlineData("text/html", 1)]
    [InlineData("application/pdf", 1)]
    [InlineData("video/mp4", 1)]   // a video sent as an image
    [InlineData("image/png", 2)]   // an image sent as a video
    public async Task InitializeUploadAsync_RefusesActiveOrMismatchedContent(string contentType, int fileType)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.InitializeUploadAsync("x", contentType, 1024, fileType, _shopId, _userId));
    }

    [Fact]
    public async Task InitializeUploadAsync_IgnoresContentTypeParametersAndCase()
    {
        var result = await _service.InitializeUploadAsync("a.png", "Image/PNG; charset=binary", 1024, 1, _shopId, _userId);

        Assert.Equal("image/png", result.ContentType);
    }

    #endregion

    #region Chunks

    [Fact]
    public async Task UploadChunkAsync_StoresTheBytes()
    {
        var file = PendingFile("image/png", 1024, 1);
        var bytes = Png(1024);

        await UploadAsync(file, 0, bytes);

        var chunk = Assert.Single(_chunks);
        Assert.Equal(2, chunk.Status);  // Uploaded
        Assert.Equal(bytes, _storage.Files[chunk.StoragePath]);
    }

    [Fact]
    public async Task UploadChunkAsync_WithChecksumMismatch_ShouldThrowAndStoreNothing()
    {
        var file = PendingFile("image/png", 1024, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UploadChunkAsync(file.Id, 0, new MemoryStream(Png(1024)), "00000000000000000000000000000000", _shopId));

        Assert.Empty(_storage.Files);
    }

    [Fact]
    public async Task UploadChunkAsync_SendingAChunkAgainReplacesIt()
    {
        var file = PendingFile("image/png", 1024, 1);

        await UploadAsync(file, 0, Png(1024));
        var second = Png(1024);
        second[100] = 7;
        await UploadAsync(file, 0, second);

        var chunk = Assert.Single(_chunks);
        Assert.Equal(second, _storage.Files[chunk.StoragePath]);
    }

    [Fact]
    public async Task UploadChunkAsync_EmptyChunk_ShouldThrow()
    {
        var file = PendingFile("image/png", 1024, 1);

        await Assert.ThrowsAsync<ArgumentException>(() => UploadAsync(file, 0, Array.Empty<byte>()));
    }

    #endregion

    #region Complete

    [Fact]
    public async Task CompleteUploadAsync_AssemblesChunksInOrder_AndActivatesTheFile()
    {
        var first = Png(100);
        var second = Enumerable.Range(0, 60).Select(i => (byte)i).ToArray();
        var file = PendingFile("image/png", first.Length + second.Length, 1);

        await UploadAsync(file, 1, second);   // out of order on purpose
        await UploadAsync(file, 0, first);
        var result = await _service.CompleteUploadAsync(file.Id, _shopId);

        Assert.Equal(2, result.Status);  // Active
        Assert.Equal(first.Concat(second).ToArray(), _storage.Files[file.StoragePath]);
        Assert.DoesNotContain(_storage.Files.Keys, k => k.Contains("chunk_"));   // temporary pieces are removed
    }

    [Fact]
    public async Task CompleteUploadAsync_WithMissingChunks_ShouldThrow()
    {
        var file = PendingFile("image/png", 1024, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteUploadAsync(file.Id, _shopId));
    }

    [Fact]
    public async Task CompleteUploadAsync_WithAGapInTheChunks_ShouldThrow()
    {
        var file = PendingFile("image/png", 200, 1);
        await UploadAsync(file, 0, Png(100));
        await UploadAsync(file, 2, new byte[100]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteUploadAsync(file.Id, _shopId));
    }

    [Fact]
    public async Task CompleteUploadAsync_WhenSizeDiffersFromAnnounced_ShouldThrow()
    {
        var file = PendingFile("image/png", 500, 1);
        await UploadAsync(file, 0, Png(100));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteUploadAsync(file.Id, _shopId));
        Assert.Contains("size", ex.Message);
    }

    [Fact]
    public async Task CompleteUploadAsync_RejectsContentThatIsNotTheDeclaredType()
    {
        var file = PendingFile("image/png", 100, 1);
        await UploadAsync(file, 0, System.Text.Encoding.ASCII.GetBytes("<html><script>alert(1)</script></html>".PadRight(100)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CompleteUploadAsync(file.Id, _shopId));

        Assert.Equal(3, file.Status);  // Failed
        Assert.Empty(_storage.Files);   // nothing is kept
    }

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("image/webp")]
    [InlineData("image/jpeg")]
    [InlineData("image/gif")]
    [InlineData("video/webm")]
    public async Task CompleteUploadAsync_AcceptsGenuineHeaders(string contentType)
    {
        byte[] header = contentType switch
        {
            "video/mp4" => new byte[] { 0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p', (byte)'i', (byte)'s', (byte)'o', (byte)'m' },
            "image/webp" => System.Text.Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBPVP8 "),
            "image/jpeg" => new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10 },
            "image/gif" => System.Text.Encoding.ASCII.GetBytes("GIF89a......"),
            _ => new byte[] { 0x1A, 0x45, 0xDF, 0xA3, 1, 0, 0, 0 }
        };
        var file = PendingFile(contentType, header.Length, contentType.StartsWith("video/") ? 2 : 1);
        await UploadAsync(file, 0, header);

        var result = await _service.CompleteUploadAsync(file.Id, _shopId);

        Assert.Equal(2, result.Status);
    }

    #endregion

    #region URLs and download

    [Fact]
    public async Task GetMediaUrlAsync_WithActiveFile_ShouldGenerateSignedUrl()
    {
        var file = PendingFile("video/mp4", 1024, 2);
        file.MarkAsActive();

        var result = await _service.GetMediaUrlAsync(file.Id, _shopId, 60);

        Assert.Equal(file.Id, result.MediaFileId);
        Assert.StartsWith($"/api/media/{file.Id}/download?expires=", result.Url);
        Assert.Contains("&sig=", result.Url);
        Assert.NotNull(result.ExpiresAt);
    }

    [Fact]
    public async Task GetMediaUrlAsync_WithPendingFile_ShouldThrow()
    {
        var file = PendingFile("video/mp4", 1024, 2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetMediaUrlAsync(file.Id, _shopId));
    }

    [Fact]
    public async Task GetMediaUrlAsync_WithExcessiveExpiration_ShouldThrow()
    {
        var file = PendingFile("video/mp4", 1024, 2);
        file.MarkAsActive();

        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMediaUrlAsync(file.Id, _shopId, 43200 + 10000));
    }

    private async Task<(MediaFile file, long expires, string sig, byte[] bytes)> ActiveFileWithLinkAsync()
    {
        var bytes = Png(300);
        var file = PendingFile("image/png", bytes.Length, 1);
        await UploadAsync(file, 0, bytes);
        await _service.CompleteUploadAsync(file.Id, _shopId);

        var url = (await _service.GetMediaUrlAsync(file.Id, _shopId, 30)).Url;
        var query = System.Web.HttpUtility.ParseQueryString(url[(url.IndexOf('?') + 1)..]);
        return (file, long.Parse(query["expires"]!), query["sig"]!, bytes);
    }

    [Fact]
    public async Task OpenDownloadAsync_WithTheSignedLink_ReturnsTheFile()
    {
        var (file, expires, sig, bytes) = await ActiveFileWithLinkAsync();

        await using var download = await _service.OpenDownloadAsync(file.Id, expires, sig);

        Assert.NotNull(download);
        Assert.Equal("image/png", download!.ContentType);
        using var ms = new MemoryStream();
        await download.Content.CopyToAsync(ms);
        Assert.Equal(bytes, ms.ToArray());
    }

    [Fact]
    public async Task OpenDownloadAsync_RefusesTamperedExpiredOrForeignLinks()
    {
        var (file, expires, sig, _) = await ActiveFileWithLinkAsync();

        Assert.Null(await _service.OpenDownloadAsync(file.Id, expires, sig + "x"));                      // altered signature
        Assert.Null(await _service.OpenDownloadAsync(file.Id, expires + 3600, sig));                     // extended expiry
        Assert.Null(await _service.OpenDownloadAsync(file.Id, DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(), sig));
        Assert.Null(await _service.OpenDownloadAsync(Guid.NewGuid(), expires, sig));                     // another file
        Assert.Null(await _service.OpenDownloadAsync(file.Id, expires, null));
    }

    [Fact]
    public void Signer_UsesTheConfiguredKey()
    {
        var other = new MediaUrlSigner(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Media:SigningKey"] = "a-completely-different-key-0000000000" })
            .Build());
        var id = Guid.NewGuid();
        var expires = DateTime.UtcNow.AddMinutes(5);
        var unix = new DateTimeOffset(expires).ToUnixTimeSeconds();

        Assert.True(_signer.Verify(id, unix, _signer.Sign(id, expires), DateTime.UtcNow));
        Assert.False(other.Verify(id, unix, _signer.Sign(id, expires), DateTime.UtcNow));
    }

    #endregion
}
