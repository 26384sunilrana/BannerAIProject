namespace BannerService.Infrastructure.Storage;

public interface IStorageProvider
{
    Task SaveChunkAsync(string path, Stream data);
    Task<Stream> GetChunkAsync(string path);
    Task<bool> ChunkExistsAsync(string path);
    Task DeleteChunkAsync(string path);
    Task<byte[]> GetFileAsync(string path);
}

public class LocalStorageProvider : IStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalStorageProvider> _logger;

    public LocalStorageProvider(IConfiguration configuration, ILogger<LocalStorageProvider> logger)
    {
        _basePath = configuration["MediaService:LocalStoragePath"] ?? "./storage";
        _logger = logger;

        // Ensure directory exists
        Directory.CreateDirectory(_basePath);
    }

    public async Task SaveChunkAsync(string path, Stream data)
    {
        var fullPath = Path.Combine(_basePath, path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await data.CopyToAsync(fileStream);
        }

        _logger.LogInformation($"Chunk saved: {path}");
    }

    public async Task<Stream> GetChunkAsync(string path)
    {
        var fullPath = Path.Combine(_basePath, path);

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
        var fullPath = Path.Combine(_basePath, path);
        return await Task.FromResult(File.Exists(fullPath));
    }

    public async Task DeleteChunkAsync(string path)
    {
        var fullPath = Path.Combine(_basePath, path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation($"Chunk deleted: {path}");
        }

        await Task.CompletedTask;
    }

    public async Task<byte[]> GetFileAsync(string path)
    {
        var fullPath = Path.Combine(_basePath, path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {path}");

        return await File.ReadAllBytesAsync(fullPath);
    }
}
