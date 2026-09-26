namespace BannerService.Domain.ValueObjects;

public class MediaUrl
{
    public string Url { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
    public bool IsPublic { get; set; }
    public string? Token { get; set; }
    public DateTime GeneratedAt { get; set; }

    public MediaUrl() { }

    public MediaUrl(string url, DateTime? expiresAt = null, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Url required");

        Url = url;
        ExpiresAt = expiresAt;
        Token = token;
        IsPublic = string.IsNullOrEmpty(token);
        GeneratedAt = DateTime.UtcNow;
    }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt;
}
