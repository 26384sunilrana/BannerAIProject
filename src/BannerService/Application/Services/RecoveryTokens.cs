namespace BannerService.Application.Services;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// One-time tokens for password reset and e-mail verification (ADR-007). The token itself goes only into the e-mail; the database keeps
/// its SHA-256 hash, so a copy of the database cannot be used to take over an account.
/// </summary>
public static class RecoveryTokens
{
    /// <summary>32 random bytes, in a form that is safe inside a link.</summary>
    public static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()))).ToLowerInvariant();
}
