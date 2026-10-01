namespace BannerService.Infrastructure.Security;

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// Application-level encryption for personal and payment fields (phone numbers, addresses, payment
/// references). Values are stored as "enc:v1:&lt;protected payload&gt;". Plain values written before
/// encryption was switched on are still readable and become encrypted the next time they are saved.
/// </summary>
public static class FieldEncryption
{
    public const string Prefix = "enc:v1:";
    public const string Purpose = "BannerAI.Fields.v1";

    private static IDataProtector? _protector;

    public static bool IsConfigured => _protector != null;

    /// <summary>Called once at start-up. Passing null turns encryption off (tests, local development).</summary>
    public static void Configure(IDataProtectionProvider? provider) =>
        _protector = provider?.CreateProtector(Purpose);

    public static string Encrypt(string plain)
    {
        if (_protector == null || plain.StartsWith(Prefix, StringComparison.Ordinal))
            return plain;

        return Prefix + _protector.Protect(plain);
    }

    public static string Decrypt(string stored)
    {
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
            return stored; // written before encryption was enabled

        if (_protector == null)
            throw new CryptographicException("Encrypted data was found but field encryption is not configured");

        return _protector.Unprotect(stored[Prefix.Length..]);
    }
}

public class EncryptedStringConverter : ValueConverter<string, string>
{
    public EncryptedStringConverter()
        : base(plain => FieldEncryption.Encrypt(plain), stored => FieldEncryption.Decrypt(stored))
    {
    }
}
