using BannerService.Domain.Services;
using System.Security.Claims;
using Xunit;

namespace BannerService.Application.Tests.Authentication
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _service;

        public JwtTokenServiceTests()
        {
            var jwtSettings = new JwtTokenService.JwtSettings
            {
                SecretKey = "this_is_a_very_long_secret_key_for_jwt_token_generation_and_validation_purposes_only_12345",
                Issuer = "BannerAIProject",
                Audience = "BannerAIProjectUsers",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            };
            _service = new JwtTokenService(jwtSettings);
        }

        [Fact]
        public void GenerateAccessToken_WithValidClaims_ReturnsToken()
        {
            // Arrange
            var userId = "user-123";
            var email = "test@example.com";
            var fullName = "John Doe";
            var shopId = "shop-456";
            var roles = new[] { "SalesExecutive" };

            // Act
            var token = _service.GenerateAccessToken(userId, email, fullName, shopId, roles);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.Contains(".", token); // JWT has dots separating parts
        }

        [Fact]
        public void GenerateRefreshToken_ReturnsSecureToken()
        {
            // Act
            var token = _service.GenerateRefreshToken();

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
            Assert.True(token.Length > 40); // Should be reasonably long
        }

        [Fact]
        public void GenerateRefreshToken_GeneratesDifferentTokens()
        {
            // Act
            var token1 = _service.GenerateRefreshToken();
            var token2 = _service.GenerateRefreshToken();

            // Assert
            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void ValidateToken_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var userId = "user-123";
            var email = "test@example.com";
            var fullName = "John Doe";
            var shopId = "shop-456";
            var roles = new[] { "SalesExecutive" };
            var token = _service.GenerateAccessToken(userId, email, fullName, shopId, roles);

            // Act
            var isValid = _service.ValidateToken(token);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var isValid = _service.ValidateToken(invalidToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_WithEmptyToken_ReturnsFalse()
        {
            // Act
            var isValid = _service.ValidateToken(string.Empty);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_WithNullToken_ReturnsFalse()
        {
            // Act
            var isValid = _service.ValidateToken(null!);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void ValidateToken_WithMalformedToken_ReturnsFalse()
        {
            // Arrange
            var malformedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.malformed.invalid";

            // Act
            var isValid = _service.ValidateToken(malformedToken);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_WithValidToken_ExtractsClaims()
        {
            // Arrange
            var userId = "user-123";
            var email = "test@example.com";
            var fullName = "John Doe";
            var shopId = "shop-456";
            var roles = new[] { "SalesExecutive" };
            var token = _service.GenerateAccessToken(userId, email, fullName, shopId, roles);

            // Act
            var principal = _service.GetPrincipalFromExpiredToken(token);

            // Assert
            Assert.NotNull(principal);
            Assert.NotNull(principal.Claims);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            var emailClaim = principal.FindFirst(ClaimTypes.Email);
            var nameClaim = principal.FindFirst(ClaimTypes.Name);

            Assert.NotNull(userIdClaim);
            Assert.Equal(userId, userIdClaim.Value);
            Assert.NotNull(emailClaim);
            Assert.Equal(email, emailClaim.Value);
            Assert.NotNull(nameClaim);
            Assert.Equal(fullName, nameClaim.Value);
        }

        [Fact]
        public void GetPrincipalFromExpiredToken_WithInvalidToken_ReturnsNull()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var principal = _service.GetPrincipalFromExpiredToken(invalidToken);

            // Assert
            Assert.Null(principal);
        }

        [Fact]
        public void GenerateAccessToken_IncludesAllClaims()
        {
            // Arrange
            var userId = "user-123";
            var email = "test@example.com";
            var fullName = "John Doe";
            var shopId = "shop-456";
            var roles = new[] { "SalesExecutive", "Admin" };
            var token = _service.GenerateAccessToken(userId, email, fullName, shopId, roles);

            // Act
            var principal = _service.GetPrincipalFromExpiredToken(token);

            // Assert
            Assert.NotNull(principal);
            Assert.Equal(userId, principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal(email, principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal(fullName, principal.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal(shopId, principal.FindFirst("ShopId")?.Value);

            var roleClaims = principal.FindAll(ClaimTypes.Role);
            Assert.Equal(2, roleClaims.Count());
            Assert.Contains(roleClaims, r => r.Value == "SalesExecutive");
            Assert.Contains(roleClaims, r => r.Value == "Admin");
        }

        [Fact]
        public void GenerateAccessToken_WithMultipleRoles_IncludesAllRoles()
        {
            // Arrange
            var userId = "user-123";
            var email = "test@example.com";
            var fullName = "John Doe";
            var shopId = "shop-456";
            var roles = new[] { "SalesExecutive", "ShopOwner", "Admin" };
            var token = _service.GenerateAccessToken(userId, email, fullName, shopId, roles);

            // Act
            var principal = _service.GetPrincipalFromExpiredToken(token);

            // Assert
            var roleClaims = principal!.FindAll(ClaimTypes.Role);
            Assert.Equal(3, roleClaims.Count());
            Assert.Contains(roleClaims, r => r.Value == "SalesExecutive");
            Assert.Contains(roleClaims, r => r.Value == "ShopOwner");
            Assert.Contains(roleClaims, r => r.Value == "Admin");
        }

        [Fact]
        public void GenerateAccessToken_WithEmptyRoles_StillGeneratesToken()
        {
            // Arrange
            var userId = "user-123";
            var email = "test@example.com";
            var fullName = "John Doe";
            var shopId = "shop-456";
            var roles = Array.Empty<string>();

            // Act
            var token = _service.GenerateAccessToken(userId, email, fullName, shopId, roles);

            // Assert
            Assert.NotNull(token);
            Assert.NotEmpty(token);
        }

        [Fact]
        public void ValidateToken_WithWrongSecret_ReturnsFalse()
        {
            // Arrange
            var userId = "user-123";
            var email = "test@example.com";
            var fullName = "John Doe";
            var shopId = "shop-456";
            var roles = new[] { "SalesExecutive" };

            var jwtSettings1 = new JwtTokenService.JwtSettings
            {
                SecretKey = "secret_key_one_very_long_string_for_testing_purposes_1234567890123456",
                Issuer = "BannerAIProject",
                Audience = "BannerAIProjectUsers",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            };
            var service1 = new JwtTokenService(jwtSettings1);
            var token = service1.GenerateAccessToken(userId, email, fullName, shopId, roles);

            var jwtSettings2 = new JwtTokenService.JwtSettings
            {
                SecretKey = "different_secret_key_very_long_string_for_testing_purposes_12345",
                Issuer = "BannerAIProject",
                Audience = "BannerAIProjectUsers",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            };
            var service2 = new JwtTokenService(jwtSettings2);

            // Act
            var isValid = service2.ValidateToken(token);

            // Assert
            Assert.False(isValid);
        }
    }
}
