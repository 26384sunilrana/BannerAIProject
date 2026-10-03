namespace BannerService.Domain.Services;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// One-time codes from an authenticator app (RFC 6238, the kind Google Authenticator, Microsoft Authenticator and Authy make): six digits,
/// a new one every 30 seconds, from a secret the server and the phone both hold.
/// </summary>
public static class Totp
{
    public const int Digits = 6;
    public const int PeriodSeconds = 30;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string GenerateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(20));

    public static long StepAt(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds() / PeriodSeconds;

    public static string CodeFor(string secret, long step)
    {
        var counter = BitConverter.GetBytes(step);
        if (BitConverter.IsLittleEndian) Array.Reverse(counter);

        using var hmac = new HMACSHA1(Base32Decode(secret));
        var hash = hmac.ComputeHash(counter);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % (int)Math.Pow(10, Digits)).ToString().PadLeft(Digits, '0');
    }

    /// <summary>
    /// Checks a typed code against the current step and one step either side (clocks differ a little). A step that was already used
    /// (<paramref name="lastUsedStep"/>) or an older one is refused, so a code seen over a shoulder or in a log cannot be used twice.
    /// </summary>
    public static bool Verify(string secret, string? code, DateTime nowUtc, long lastUsedStep, out long matchedStep)
    {
        matchedStep = 0;
        var typed = new string((code ?? string.Empty).Where(char.IsDigit).ToArray());
        if (typed.Length != Digits || string.IsNullOrEmpty(secret)) return false;

        var current = StepAt(nowUtc);
        for (var step = current - 1; step <= current + 1; step++)
        {
            if (step <= lastUsedStep) continue;
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(CodeFor(secret, step)), Encoding.ASCII.GetBytes(typed)))
            {
                matchedStep = step;
                return true;
            }
        }
        return false;
    }

    /// <summary>The address an authenticator app reads from the QR picture.</summary>
    public static string Uri(string issuer, string account, string secret) =>
        $"otpauth://totp/{System.Uri.EscapeDataString(issuer)}:{System.Uri.EscapeDataString(account)}?secret={secret}&issuer={System.Uri.EscapeDataString(issuer)}&digits={Digits}&period={PeriodSeconds}";

    public static string Base32Encode(byte[] data)
    {
        var result = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                result.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0) result.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        return result.ToString();
    }

    public static byte[] Base32Decode(string text)
    {
        var clean = text.Replace(" ", string.Empty).Replace("-", string.Empty).TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in clean)
        {
            var value = Alphabet.IndexOf(c);
            if (value < 0) throw new FormatException("Not a valid secret.");
            buffer = (buffer << 5) | value;
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }
        return bytes.ToArray();
    }

    /// <summary>Ten one-time recovery codes such as <c>K7QF2-9XM4B</c>, for when the phone is lost.</summary>
    public static List<string> GenerateRecoveryCodes(int count = 10)
    {
        var codes = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var text = Base32Encode(RandomNumberGenerator.GetBytes(7))[..10];
            codes.Add($"{text[..5]}-{text[5..]}");
        }
        return codes;
    }

    public static string HashRecoveryCode(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant())));
}
