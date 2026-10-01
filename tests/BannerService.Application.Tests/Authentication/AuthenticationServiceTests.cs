using BannerService.Application.DTOs;
using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Domain.Services;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Authentication
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IRoleRepository> _roleRepositoryMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly PasswordHashService _passwordHashService;
        private readonly JwtTokenService _jwtTokenService;
        private readonly AuthenticationService _authenticationService;

        public AuthenticationServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _roleRepositoryMock = new Mock<IRoleRepository>();
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            _emailServiceMock = new Mock<IEmailService>();
            _passwordHashService = new PasswordHashService();

            var jwtSettings = new JwtTokenService.JwtSettings
            {
                SecretKey = "this_is_a_very_long_secret_key_for_jwt_token_generation_and_validation_purposes_only_12345",
                Issuer = "BannerAIProject",
                Audience = "BannerAIProjectUsers",
                AccessTokenExpirationMinutes = 15,
                RefreshTokenExpirationDays = 7
            };
            _jwtTokenService = new JwtTokenService(jwtSettings);

            _authenticationService = new AuthenticationService(
                _userRepositoryMock.Object,
                _roleRepositoryMock.Object,
                _refreshTokenRepositoryMock.Object,
                _emailServiceMock.Object,
                _passwordHashService,
                _jwtTokenService);
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_CreatesUserSuccessfully()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "newuser@example.com",
                Password = "SecurePassword123!",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };

            var role = new Role { Id = "3", Name = "SalesExecutive", IsActive = true };
            _roleRepositoryMock.Setup(r => r.GetByNameAsync("SalesExecutive"))
                .ReturnsAsync(role);
            _userRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            _userRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) =>
                {
                    u.Id = "user-123";
                    return u;
                });
            _refreshTokenRepositoryMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
                .ReturnsAsync((RefreshToken rt) =>
                {
                    rt.Id = Guid.NewGuid().ToString();
                    return rt;
                });

            // Act
            var result = await _authenticationService.RegisterAsync(registerDto, "127.0.0.1", "Mozilla/5.0");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.True(result.ExpiresIn > 0);
            Assert.Equal("Bearer", result.TokenType);
            _userRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ThrowsException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                Password = "SecurePassword123!",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };

            _userRepositoryMock.Setup(r => r.ExistsAsync("existing@example.com"))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authenticationService.RegisterAsync(registerDto, "127.0.0.1", "Mozilla/5.0"));
        }

        [Fact]
        public async Task RegisterAsync_WithoutSalesExecutiveRole_ThrowsException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "newuser@example.com",
                Password = "SecurePassword123!",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };

            _userRepositoryMock.Setup(r => r.ExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            _roleRepositoryMock.Setup(r => r.GetByNameAsync("SalesExecutive"))
                .ReturnsAsync((Role)null!);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authenticationService.RegisterAsync(registerDto, "127.0.0.1", "Mozilla/5.0"));
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsAuthToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user@example.com",
                Password = "SecurePassword123!"
            };

            var passwordHash = new PasswordHashService().HashPassword(loginDto.Password);
            var user = new User
            {
                Id = "user-123",
                Email = loginDto.Email,
                PasswordHash = passwordHash,
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123",
                IsActive = true,
                IsLockedOut = false,
                EmailVerified = true
            };

            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = "3" });

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _refreshTokenRepositoryMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
                .ReturnsAsync((RefreshToken rt) =>
                {
                    rt.Id = Guid.NewGuid().ToString();
                    return rt;
                });

            // Act
            var result = await _authenticationService.LoginAsync(loginDto, "127.0.0.1", "Mozilla/5.0");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithNonexistentUser_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "AnyPassword123!"
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync((User)null!);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authenticationService.LoginAsync(loginDto, "127.0.0.1", "Mozilla/5.0"));
        }

        [Fact]
        public async Task LoginAsync_WithIncorrectPassword_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user@example.com",
                Password = "WrongPassword123!"
            };

            var passwordHash = new PasswordHashService().HashPassword("CorrectPassword123!");
            var user = new User
            {
                Id = "user-123",
                Email = loginDto.Email,
                PasswordHash = passwordHash,
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123",
                IsActive = true,
                IsLockedOut = false,
                EmailVerified = true
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authenticationService.LoginAsync(loginDto, "127.0.0.1", "Mozilla/5.0"));
        }

        [Fact]
        public async Task LoginAsync_WithLockedOutAccount_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user@example.com",
                Password = "SecurePassword123!"
            };

            var user = new User
            {
                Id = "user-123",
                Email = loginDto.Email,
                PasswordHash = "somehash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123",
                IsActive = true,
                IsLockedOut = true
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authenticationService.LoginAsync(loginDto, "127.0.0.1", "Mozilla/5.0"));
        }

        [Fact]
        public async Task LoginAsync_WithInactiveUser_ThrowsException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user@example.com",
                Password = "SecurePassword123!"
            };

            var user = new User
            {
                Id = "user-123",
                Email = loginDto.Email,
                PasswordHash = "somehash",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123",
                IsActive = false,
                IsLockedOut = false
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authenticationService.LoginAsync(loginDto, "127.0.0.1", "Mozilla/5.0"));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithValidToken_ReturnsNewTokens()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "valid-refresh-token"
            };

            var user = new User
            {
                Id = "user-123",
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                ShopId = "shop-123"
            };

            var oldRefreshToken = new RefreshToken
            {
                Id = "rt-123",
                UserId = user.Id,
                Token = "valid-refresh-token",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                JwtId = "old-jwt-id",
                User = user
            };

            _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync(refreshTokenDto.RefreshToken))
                .ReturnsAsync(oldRefreshToken);
            _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id))
                .ReturnsAsync(user);
            _refreshTokenRepositoryMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>()))
                .ReturnsAsync((RefreshToken rt) =>
                {
                    rt.Id = Guid.NewGuid().ToString();
                    return rt;
                });

            // Act
            var result = await _authenticationService.RefreshTokenAsync(refreshTokenDto, "127.0.0.1", "Mozilla/5.0");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(oldRefreshToken), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithInvalidToken_ThrowsException()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "invalid-refresh-token"
            };

            _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync(refreshTokenDto.RefreshToken))
                .ReturnsAsync((RefreshToken)null!);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authenticationService.RefreshTokenAsync(refreshTokenDto, "127.0.0.1", "Mozilla/5.0"));
        }

        [Fact]
        public async Task RefreshTokenAsync_WithRevokedToken_ThrowsException()
        {
            // Arrange
            var refreshTokenDto = new RefreshTokenDto
            {
                RefreshToken = "revoked-refresh-token"
            };

            var revokedRefreshToken = new RefreshToken
            {
                Id = "rt-123",
                Token = "revoked-refresh-token",
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow
            };

            _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync(refreshTokenDto.RefreshToken))
                .ReturnsAsync(revokedRefreshToken);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _authenticationService.RefreshTokenAsync(refreshTokenDto, "127.0.0.1", "Mozilla/5.0"));
        }

        [Fact]
        public async Task RevokeTokenAsync_WithValidToken_RevokesSuccessfully()
        {
            // Arrange
            var revokeDto = new RefreshTokenDto
            {
                RefreshToken = "valid-token"
            };

            var refreshToken = new RefreshToken
            {
                Id = "rt-123",
                Token = "valid-token",
                IsRevoked = false
            };

            _refreshTokenRepositoryMock.Setup(r => r.GetByTokenAsync(revokeDto.RefreshToken))
                .ReturnsAsync(refreshToken);

            // Act
            await _authenticationService.RevokeTokenAsync(revokeDto);

            // Assert
            Assert.True(refreshToken.IsRevoked);
            _refreshTokenRepositoryMock.Verify(r => r.UpdateAsync(refreshToken), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailAsync_WithValidToken_VerifiesEmailSuccessfully()
        {
            // Arrange
            var verifyDto = new VerifyEmailDto
            {
                VerificationToken = "valid-verification-token"
            };

            var user = new User
            {
                Id = "user-123",
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                VerificationToken = "valid-verification-token",
                VerificationTokenExpires = DateTime.UtcNow.AddHours(1),
                EmailVerified = false
            };

            _userRepositoryMock.Setup(r => r.GetByVerificationTokenAsync(verifyDto.VerificationToken))
                .ReturnsAsync(user);

            // Act
            var result = await _authenticationService.VerifyEmailAsync(verifyDto);

            // Assert
            Assert.True(result);
            Assert.True(user.EmailVerified);
            Assert.Null(user.VerificationToken);
            _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task VerifyEmailAsync_WithExpiredToken_ReturnsFalse()
        {
            // Arrange
            var verifyDto = new VerifyEmailDto
            {
                VerificationToken = "expired-token"
            };

            var user = new User
            {
                Id = "user-123",
                Email = "user@example.com",
                VerificationToken = "expired-token",
                VerificationTokenExpires = DateTime.UtcNow.AddHours(-1),
                EmailVerified = false
            };

            _userRepositoryMock.Setup(r => r.GetByVerificationTokenAsync(verifyDto.VerificationToken))
                .ReturnsAsync(user);

            // Act
            var result = await _authenticationService.VerifyEmailAsync(verifyDto);

            // Assert
            Assert.False(result);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_WithValidEmail_ReturnsTrue()
        {
            // Arrange
            var requestDto = new RequestPasswordResetDto
            {
                Email = "user@example.com"
            };

            var user = new User
            {
                Id = "user-123",
                Email = requestDto.Email,
                FirstName = "John",
                LastName = "Doe"
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(requestDto.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _authenticationService.RequestPasswordResetAsync(requestDto);

            // Assert
            Assert.True(result);
            Assert.NotNull(user.PasswordResetToken);
            Assert.NotNull(user.PasswordResetExpires);
            _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task RequestPasswordResetAsync_WithInvalidEmail_ReturnsTrue()
        {
            // Arrange - Note: Returns true for security reasons (doesn't reveal if email exists)
            var requestDto = new RequestPasswordResetDto
            {
                Email = "nonexistent@example.com"
            };

            _userRepositoryMock.Setup(r => r.GetByEmailAsync(requestDto.Email))
                .ReturnsAsync((User)null!);

            // Act
            var result = await _authenticationService.RequestPasswordResetAsync(requestDto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithValidToken_ResetsPasswordSuccessfully()
        {
            // Arrange
            var resetDto = new ResetPasswordDto
            {
                ResetToken = "valid-reset-token",
                NewPassword = "NewPassword123!"
            };

            var user = new User
            {
                Id = "user-123",
                Email = "user@example.com",
                PasswordResetToken = "valid-reset-token",
                PasswordResetExpires = DateTime.UtcNow.AddMinutes(30)
            };

            _userRepositoryMock.Setup(r => r.GetByPasswordResetTokenAsync(resetDto.ResetToken))
                .ReturnsAsync(user);

            // Act
            var result = await _authenticationService.ResetPasswordAsync(resetDto);

            // Assert
            Assert.True(result);
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.PasswordResetExpires);
            _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithExpiredToken_ReturnsFalse()
        {
            // Arrange
            var resetDto = new ResetPasswordDto
            {
                ResetToken = "expired-token",
                NewPassword = "NewPassword123!"
            };

            var user = new User
            {
                Id = "user-123",
                Email = "user@example.com",
                PasswordResetToken = "expired-token",
                PasswordResetExpires = DateTime.UtcNow.AddMinutes(-10)
            };

            _userRepositoryMock.Setup(r => r.GetByPasswordResetTokenAsync(resetDto.ResetToken))
                .ReturnsAsync(user);

            // Act
            var result = await _authenticationService.ResetPasswordAsync(resetDto);

            // Assert
            Assert.False(result);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var resetDto = new ResetPasswordDto
            {
                ResetToken = "invalid-token",
                NewPassword = "NewPassword123!"
            };

            _userRepositoryMock.Setup(r => r.GetByPasswordResetTokenAsync(resetDto.ResetToken))
                .ReturnsAsync((User)null!);

            // Act
            var result = await _authenticationService.ResetPasswordAsync(resetDto);

            // Assert
            Assert.False(result);
        }
    }
}
