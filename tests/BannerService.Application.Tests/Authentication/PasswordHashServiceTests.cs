using BannerService.Domain.Services;
using Xunit;

namespace BannerService.Application.Tests.Authentication
{
    public class PasswordHashServiceTests
    {
        private readonly PasswordHashService _service;

        public PasswordHashServiceTests()
        {
            _service = new PasswordHashService();
        }

        [Fact]
        public void HashPassword_WithValidPassword_ReturnsHash()
        {
            // Arrange
            var password = "SecurePassword123!";

            // Act
            var hash = _service.HashPassword(password);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEqual(password, hash);
            Assert.True(hash.Length > 0);
        }

        [Fact]
        public void HashPassword_WithSamePassword_ReturnsDifferentHashes()
        {
            // Arrange
            var password = "SecurePassword123!";

            // Act
            var hash1 = _service.HashPassword(password);
            var hash2 = _service.HashPassword(password);

            // Assert
            Assert.NotEqual(hash1, hash2); // Different salts
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
        {
            // Arrange
            var password = "SecurePassword123!";
            var hash = _service.HashPassword(password);

            // Act
            var isValid = _service.VerifyPassword(password, hash);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
        {
            // Arrange
            var password = "SecurePassword123!";
            var wrongPassword = "WrongPassword456@";
            var hash = _service.HashPassword(password);

            // Act
            var isValid = _service.VerifyPassword(wrongPassword, hash);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
        {
            // Arrange
            var password = "SecurePassword123!";
            var hash = _service.HashPassword(password);

            // Act
            var isValid = _service.VerifyPassword(string.Empty, hash);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void VerifyPassword_WithNullHash_ReturnsFalse()
        {
            // Arrange
            var password = "SecurePassword123!";

            // Act
            var isValid = _service.VerifyPassword(password, null!);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void VerifyPassword_WithInvalidHash_ReturnsFalse()
        {
            // Arrange
            var password = "SecurePassword123!";
            var invalidHash = "not-a-valid-hash";

            // Act
            var isValid = _service.VerifyPassword(password, invalidHash);

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void HashPassword_WithInvalidPassword_ThrowsException(string? password)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.HashPassword(password!));
        }

        [Fact]
        public void HashPassword_DifferentSalts_VerifyBothHashes()
        {
            // Arrange
            var password = "SecurePassword123!";

            // Act
            var hash1 = _service.HashPassword(password);
            var hash2 = _service.HashPassword(password);

            // Assert
            Assert.NotEqual(hash1, hash2);
            Assert.True(_service.VerifyPassword(password, hash1));
            Assert.True(_service.VerifyPassword(password, hash2));
        }

        [Fact]
        public void VerifyPassword_CaseSensitive_DifferentCases()
        {
            // Arrange
            var password = "SecurePassword123!";
            var hash = _service.HashPassword(password);

            // Act
            var lowerCaseValid = _service.VerifyPassword("securepassword123!", hash);
            var upperCaseValid = _service.VerifyPassword("SECUREPASSWORD123!", hash);

            // Assert
            Assert.False(lowerCaseValid);
            Assert.False(upperCaseValid);
        }

        [Fact]
        public void HashPassword_LongPassword_Success()
        {
            // Arrange
            var longPassword = new string('a', 500);

            // Act
            var hash = _service.HashPassword(longPassword);

            // Assert
            Assert.True(_service.VerifyPassword(longPassword, hash));
        }

        [Fact]
        public void VerifyPassword_WithWhitespacePassword_DifferentFromTrimmed()
        {
            // Arrange
            var password = "SecurePassword123!";
            var passwordWithSpace = " SecurePassword123! ";
            var hash = _service.HashPassword(password);

            // Act
            var isValid = _service.VerifyPassword(passwordWithSpace, hash);

            // Assert
            Assert.False(isValid);
        }
    }
}
