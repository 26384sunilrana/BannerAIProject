namespace BannerService.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;

public interface IMediaUrlSigner
{
    /// <summary>Signature that lets the holder of the link read one media file until it expires.</summary>
    string Sign(Guid mediaFileId, DateTime expiresAtUtc);

    bool Verify(Guid mediaFileId, long expiresUnixSeconds, string? signature, DateTime nowUtc);
}

/// <summary>
/// Signs time-limited download links so images and videos can be loaded by a browser tag, which cannot send
/// an Authorization header. The key comes from Media:SigningKey, or is derived from the JWT secret.
/// </summary>
public class MediaUrlSigner : IMediaUrlSigner
{
    private readonly byte[] _key;

    public MediaUrlSigner(IConfiguration configuration)
    {
        var secret = configuration["Media:SigningKey"]
            ?? configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Media:SigningKey or Jwt:SecretKey must be configured");

        // Derive a separate key so a media signature can never be reused as anything else
        using var derive = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        _key = derive.ComputeHash(Encoding.UTF8.GetBytes("media-url-v1"));
    }

    public string Sign(Guid mediaFileId, DateTime expiresAtUtc) =>
        Compute(mediaFileId, new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds());

    public bool Verify(Guid mediaFileId, long expiresUnixSeconds, string? signature, DateTime nowUtc)
    {
        if (string.IsNullOrEmpty(signature))
            return false;

        if (expiresUnixSeconds < new DateTimeOffset(DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc)).ToUnixTimeSeconds())
            return false;

        var expected = Encoding.ASCII.GetBytes(Compute(mediaFileId, expiresUnixSeconds));
        var actual = Encoding.ASCII.GetBytes(signature);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private string Compute(Guid mediaFileId, long expiresUnixSeconds)
    {
        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{mediaFileId:N}:{expiresUnixSeconds}"));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
