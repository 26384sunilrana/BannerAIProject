namespace BannerService.Domain.Services
{
    using System.Security.Cryptography;

    /// <summary>
    /// Makes the platform-wide identifiers for shops and for each level of the location hierarchy:
    /// a prefix and eight characters, such as SHP-7KQ2-9M4D. The characters leave out 0/O, 1/I/L and U,
    /// so an identifier read out over the phone is not mistaken. Uniqueness is guaranteed by a unique
    /// index; the caller retries on the rare collision.
    /// </summary>
    public static class UniqueIdGenerator
    {
        public const string Shop = "SHP";
        public const string Group = "GRP";
        public const string City = "CTY";
        public const string State = "STA";
        public const string Country = "CNT";

        private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";

        public static string Generate(string prefix)
        {
            Span<char> chars = stackalloc char[8];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

            return $"{prefix}-{new string(chars[..4])}-{new string(chars[4..])}";
        }

        public static bool IsValid(string? value, string prefix) =>
            value is { Length: 13 }
            && value.StartsWith(prefix + "-", StringComparison.Ordinal)
            && value[8] == '-'
            && value[4..8].Concat(value[9..]).All(c => Alphabet.Contains(c));
    }
}
