namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Services;
    using DTOs;

    public interface IAuthenticationService
    {
        Task<(bool success, string message, AuthTokenDto? tokens)> RegisterAsync(RegisterDto request);
        Task<(bool success, string message, AuthTokenDto? tokens)> LoginAsync(LoginDto request);
        Task<(bool success, string message, AuthTokenDto? tokens)> RefreshTokenAsync(string refreshToken, string userId);
        Task<bool> RevokeTokenAsync(string userId, string refreshToken);
        Task<(bool success, string message)> VerifyEmailAsync(string userId, string token);
        Task<(bool success, string message)> RequestPasswordResetAsync(string email);
        Task<(bool success, string message)> ResetPasswordAsync(string userId, string token, string newPassword);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IPasswordHashService _passwordHashService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthenticationService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IJwtTokenService jwtTokenService,
            IPasswordHashService passwordHashService,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _jwtTokenService = jwtTokenService;
            _passwordHashService = passwordHashService;
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool success, string message, AuthTokenDto? tokens)> RegisterAsync(RegisterDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return (false, "Email and password are required", null);

            if (await _userRepository.ExistsAsync(request.Email))
                return (false, "Email already registered", null);

            if (request.Password.Length < 8)
                return (false, "Password must be at least 8 characters", null);

            var user = new User
            {
                Email = request.Email.ToLower(),
                PasswordHash = _passwordHashService.HashPassword(request.Password),
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                ShopId = request.ShopId ?? string.Empty,
                IsActive = true
            };

            var createdUser = await _userRepository.CreateAsync(user);

            // Assign default role (SalesExecutive)
            await _roleRepository.AssignRoleAsync(createdUser.Id, "3");

            var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(createdUser);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = createdUser.Id,
                Token = refreshToken,
                JwtId = jwtId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IpAddress = request.IpAddress ?? string.Empty,
                UserAgent = request.UserAgent ?? string.Empty
            };

            await _unitOfWork.RefreshTokenRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var tokens = new AuthTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900, // 15 minutes
                TokenType = "Bearer"
            };

            return (true, "Registration successful", tokens);
        }

        public async Task<(bool success, string message, AuthTokenDto? tokens)> LoginAsync(LoginDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return (false, "Email and password are required", null);

            var user = await _userRepository.GetByEmailAsync(request.Email.ToLower());

            if (user == null)
                return (false, "Invalid email or password", null);

            if (!user.IsActive)
                return (false, "Account is inactive", null);

            if (user.IsLockedOut)
                return (false, "Account is locked due to multiple failed login attempts", null);

            if (!_passwordHashService.VerifyPassword(request.Password, user.PasswordHash))
            {
                user.RecordLoginAttempt();
                await _userRepository.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
                return (false, "Invalid email or password", null);
            }

            user.ResetLoginAttempts();
            await _userRepository.UpdateAsync(user);

            var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                JwtId = jwtId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IpAddress = request.IpAddress ?? string.Empty,
                UserAgent = request.UserAgent ?? string.Empty
            };

            await _unitOfWork.RefreshTokenRepository.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var tokens = new AuthTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900, // 15 minutes
                TokenType = "Bearer"
            };

            return (true, "Login successful", tokens);
        }

        public async Task<(bool success, string message, AuthTokenDto? tokens)> RefreshTokenAsync(string refreshToken, string userId)
        {
            if (string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(userId))
                return (false, "Refresh token and user ID are required", null);

            var storedRefreshToken = await _unitOfWork.RefreshTokenRepository.GetByTokenAsync(refreshToken);

            if (storedRefreshToken == null || !storedRefreshToken.IsActive || storedRefreshToken.UserId != userId)
                return (false, "Invalid refresh token", null);

            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null || !user.IsActive)
                return (false, "User not found or inactive", null);

            var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Revoke old token
            storedRefreshToken.Revoke(newRefreshToken);
            await _unitOfWork.RefreshTokenRepository.UpdateAsync(storedRefreshToken);

            // Create new token
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                JwtId = jwtId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IpAddress = storedRefreshToken.IpAddress,
                UserAgent = storedRefreshToken.UserAgent
            };

            await _unitOfWork.RefreshTokenRepository.AddAsync(newRefreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var tokens = new AuthTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 900, // 15 minutes
                TokenType = "Bearer"
            };

            return (true, "Token refreshed successfully", tokens);
        }

        public async Task<bool> RevokeTokenAsync(string userId, string refreshToken)
        {
            var token = await _unitOfWork.RefreshTokenRepository.GetByTokenAsync(refreshToken);

            if (token == null || token.UserId != userId)
                return false;

            token.Revoke();
            await _unitOfWork.RefreshTokenRepository.UpdateAsync(token);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<(bool success, string message)> VerifyEmailAsync(string userId, string token)
        {
            var user = await _userRepository.GetByVerificationTokenAsync(token);

            if (user == null || user.Id != userId)
                return (false, "Invalid verification token");

            if (user.VerificationTokenExpires < DateTime.UtcNow)
                return (false, "Verification token has expired");

            user.VerifyEmail();
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return (true, "Email verified successfully");
        }

        public async Task<(bool success, string message)> RequestPasswordResetAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email.ToLower());

            if (user == null)
                return (true, "If an account with that email exists, a password reset link has been sent");

            var resetToken = Guid.NewGuid().ToString("N");
            user.SetPasswordResetToken(resetToken);
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // TODO: Send reset email with token

            return (true, "Password reset link sent to your email");
        }

        public async Task<(bool success, string message)> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            var user = await _userRepository.GetByPasswordResetTokenAsync(token);

            if (user == null || user.Id != userId)
                return (false, "Invalid or expired password reset token");

            if (!user.IsPasswordResetTokenValid())
                return (false, "Password reset token has expired");

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return (false, "Password must be at least 8 characters");

            user.PasswordHash = _passwordHashService.HashPassword(newPassword);
            user.ClearPasswordResetToken();
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return (true, "Password reset successfully");
        }
    }
}
