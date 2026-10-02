namespace BannerService.Infrastructure.Storage;

public interface IStorageProvider
{
    Task SaveChunkAsync(string path, Stream data);
    Task<Stream> GetChunkAsync(string path);
    Task<bool> ChunkExistsAsync(string path);
    Task DeleteChunkAsync(string path);
    Task<byte[]> GetFileAsync(string path);

    /// <summary>Adds the data to the end of the file, creating it if needed (used to assemble uploaded chunks).</summary>
    Task AppendAsync(string path, Stream data);

    /// <summary>Opens a stored file for streaming; the caller disposes the stream.</summary>
    Task<Stream> OpenReadAsync(string path);
}

public class LocalStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalStorageProvider> _logger;

    public LocalStorageProvider(IConfiguration configuration, ILogger<LocalStorageProvider> logger)
    {
        _basePath = Path.GetFullPath(configuration["MediaService:LocalStoragePath"] ?? "./storage");
        _logger = logger;

        // Ensure directory exists
        Directory.CreateDirectory(_basePath);
    }

    // Storage paths are built from ids, but a path that climbs out of the storage folder is refused anyway
    private string Resolve(string path)
    {
        var full = Path.GetFullPath(Path.Combine(_basePath, path));
        if (!full.StartsWith(_basePath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("Path is outside the storage folder");
        return full;
    }

    public async Task SaveChunkAsync(string path, Stream data)
    {
        var fullPath = Resolve(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await data.CopyToAsync(fileStream);
        }

        _logger.LogInformation("Chunk saved: {Path}", path);
    }

    public async Task AppendAsync(string path, Stream data)
    {
        var fullPath = Resolve(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var fileStream = new FileStream(fullPath, FileMode.Append, FileAccess.Write);
        await data.CopyToAsync(fileStream);
    }

    public Task<Stream> OpenReadAsync(string path)
    {
        var fullPath = Resolve(path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");

        return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920, useAsync: true));
    }

    public async Task<Stream> GetChunkAsync(string path)
    {
        var fullPath = Resolve(path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Chunk not found: {path}");

        var ms = new MemoryStream();
        using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
        {
            await fileStream.CopyToAsync(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    public async Task<bool> ChunkExistsAsync(string path)
    {
        return await Task.FromResult(File.Exists(Resolve(path)));
    }

    public async Task DeleteChunkAsync(string path)
    {
        var fullPath = Resolve(path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Chunk deleted: {Path}", path);
        }

        await Task.CompletedTask;
    }

    public async Task<byte[]> GetFileAsync(string path)
    {
        var fullPath = Resolve(path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");

        return await File.ReadAllBytesAsync(fullPath);
    }
}
