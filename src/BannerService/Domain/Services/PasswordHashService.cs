namespace BannerService.Domain.Services
{
    using System.Security.Cryptography;
    using System.Text;

    public interface IPasswordHashService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);

        /// <summary>True when the stored hash was made with older, weaker settings and should be replaced after a successful sign-in.</summary>
        bool NeedsRehash(string hash);
    }

    /// <summary>
    /// PBKDF2-SHA256 with a random salt. New hashes read "v2$iterations$salt+hash" so the strength can be raised later and old hashes
    /// still checked. Hashes made before that (plain base64, 10,000 rounds) are still accepted and are replaced at the next sign-in.
    /// </summary>
    public class PasswordHashService : IPasswordHashService
    {
        private const int SaltSize = 128 / 8;
        private const int KeySize = 256 / 8;
        public const int Iterations = 310_000;
        private const int LegacyIterations = 10_000;
        private const string Marker = "v2$";
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, Algorithm, KeySize);

            var both = new byte[SaltSize + KeySize];
            Array.Copy(salt, 0, both, 0, SaltSize);
            Array.Copy(hash, 0, both, SaltSize, KeySize);
            return $"{Marker}{Iterations}${Convert.ToBase64String(both)}";
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            if (string.IsNullOrEmpty(hash))
                return false;

            try
            {
                var (iterations, payload) = Parse(hash);
                var bytes = Convert.FromBase64String(payload);
                if (bytes.Length != SaltSize + KeySize)
                    return false;

                var salt = bytes[..SaltSize];
                var computed = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, Algorithm, KeySize);
                return CryptographicOperations.FixedTimeEquals(computed, bytes.AsSpan(SaltSize, KeySize));
            }
            catch
            {
                return false;
            }
        }

        public bool NeedsRehash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;
            try
            {
                return Parse(hash).Iterations < Iterations;
            }
            catch
            {
                return false;
            }
        }

        private static (int Iterations, string Payload) Parse(string hash)
        {
            if (!hash.StartsWith(Marker, StringComparison.Ordinal))
                return (LegacyIterations, hash);

            var parts = hash.Split('$');
            if (parts.Length != 3 || !int.TryParse(parts[1], out var rounds) || rounds < 1)
                throw new FormatException("Unknown password hash");
            return (rounds, parts[2]);
        }
    }
}
