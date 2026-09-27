namespace BannerService.Domain.Services
{
    using System.Security.Cryptography;
    using System.Text;

    public interface IPasswordHashService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class PasswordHashService : IPasswordHashService
    {
        private const int SaltSize = 128 / 8;
        private const int KeySize = 256 / 8;
        private const int Iterations = 10000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            using (var salt = RandomNumberGenerator.Create())
            {
                var saltBuffer = new byte[SaltSize];
                salt.GetBytes(saltBuffer);

                var hash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    saltBuffer,
                    Iterations,
                    Algorithm,
                    KeySize
                );

                var hashWithSalt = new byte[SaltSize + KeySize];
                Array.Copy(saltBuffer, 0, hashWithSalt, 0, SaltSize);
                Array.Copy(hash, 0, hashWithSalt, SaltSize, KeySize);

                return Convert.ToBase64String(hashWithSalt);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            if (string.IsNullOrEmpty(hash))
                return false;

            try
            {
                var hashBytes = Convert.FromBase64String(hash);

                var salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                var computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    Iterations,
                    Algorithm,
                    KeySize
                );

                for (int i = 0; i < KeySize; i++)
                {
                    if (hashBytes[SaltSize + i] != computedHash[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
