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
        /// <summary>Ends the session that holds this refresh token. False when the token is not known.</summary>
        Task<bool> RevokeTokenAsync(string refreshToken);
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
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IShopRepository _shopRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;

        /// <summary>How long a refresh token lasts (a signed-in person is asked to sign in again after this long without a visit).</summary>
        public static readonly TimeSpan RefreshLifetime = TimeSpan.FromDays(7);
        private static DateTime RefreshLifetimeEnd => DateTime.UtcNow.Add(RefreshLifetime);

        /// <summary>
        /// A refresh token that was just replaced still works for this long, and then returns the replacement. Two tabs opened at the
        /// same moment both present the same cookie; without this the second one would be signed out.
        /// </summary>
        public static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(30);

        public const string SubscriptionEndedMessage =
            "Your subscription has ended, so logins are switched off. Contact support to reactivate your account.";

        public AuthenticationService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IJwtTokenService jwtTokenService,
            IPasswordHashService passwordHashService,
            IRefreshTokenRepository refreshTokenRepository,
            IShopRepository shopRepository,
            ISubscriptionRepository subscriptionRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _jwtTokenService = jwtTokenService;
            _passwordHashService = passwordHashService;
            _refreshTokenRepository = refreshTokenRepository;
            _shopRepository = shopRepository;
            _subscriptionRepository = subscriptionRepository;
        }

        /// <summary>
        /// A shop whose subscription expired (grace week over) or was suspended cannot sign in; it only keeps the
        /// default banner it has on its own machine. Platform admins are never locked out.
        /// </summary>
        private async Task<bool> IsLockedBySubscriptionAsync(User user)
        {
            if (user.UserRoles.Any(r => r.Role?.Name == Role.Admin))
                return false;

            if (!Guid.TryParse(user.ShopId, out var shopId))
                return false;

            var subscription = await _subscriptionRepository.GetByShopIdAsync(shopId);
            return subscription?.IsLocked == true;
        }

        public async Task<(bool success, string message, AuthTokenDto? tokens)> RegisterAsync(RegisterDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return (false, "Email and password are required", null);

            if (await _userRepository.ExistsAsync(request.Email))
                return (false, "Email already registered", null);

            if (request.Password.Length < 8)
                return (false, "Password must be at least 8 characters", null);

            // Sign-up is for shop owners. Sales executives are created by the owner of their shop.
            if (!string.IsNullOrWhiteSpace(request.ShopId))
                return (false, "Logins for an existing shop are created by the shop owner", null);

            if (string.IsNullOrWhiteSpace(request.ShopName))
                return (false, "Shop name is required", null);

            var user = new User
            {
                Email = request.Email.Trim().ToLower(),
                PasswordHash = _passwordHashService.HashPassword(request.Password),
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                IsActive = true
            };

            var createdUser = await _userRepository.CreateAsync(user);

            try
            {
                var ownerId = Guid.Parse(createdUser.Id);
                var shop = await _shopRepository.CreateAsync(new Shop
                {
                    Name = request.ShopName.Trim(),
                    City = request.City,
                    PhoneNumber = request.PhoneNumber,
                    Status = ShopStatus.Active,
                    OwnerUserId = ownerId,
                    CreatedByUserId = ownerId
                });

                createdUser.ShopId = shop.Id.ToString();
                await _userRepository.UpdateAsync(createdUser);

                // The sign-up user owns the shop
                await _roleRepository.AssignRoleAsync(createdUser.Id, "2");
            }
            catch
            {
                await _userRepository.DeleteAsync(createdUser.Id);
                throw;
            }

            // Reload so the token carries the role names
            createdUser = await _userRepository.GetByEmailAsync(user.Email) ?? createdUser;

            var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(createdUser);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = createdUser.Id,
                Token = refreshToken,
                JwtId = jwtId,
                ExpiresAt = RefreshLifetimeEnd,
                IpAddress = request.IpAddress ?? string.Empty,
                UserAgent = request.UserAgent ?? string.Empty
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            var tokens = new AuthTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900, // 15 minutes
                RefreshTokenExpiresAt = RefreshLifetimeEnd,
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
                    return (false, "Invalid email or password", null);
            }

            if (await IsLockedBySubscriptionAsync(user))
                return (false, SubscriptionEndedMessage, null);

            user.ResetLoginAttempts();

            // a hash made with older, weaker settings is replaced now that the password is known
            if (_passwordHashService.NeedsRehash(user.PasswordHash))
                user.PasswordHash = _passwordHashService.HashPassword(request.Password);
            await _userRepository.UpdateAsync(user);

            var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                JwtId = jwtId,
                ExpiresAt = RefreshLifetimeEnd,
                IpAddress = request.IpAddress ?? string.Empty,
                UserAgent = request.UserAgent ?? string.Empty
            };

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            var tokens = new AuthTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = 900, // 15 minutes
                RefreshTokenExpiresAt = RefreshLifetimeEnd,
                TokenType = "Bearer"
            };

            return (true, "Login successful", tokens);
        }

        public async Task<(bool success, string message, AuthTokenDto? tokens)> RefreshTokenAsync(string refreshToken, string userId)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return (false, "Refresh token is required", null);

            var storedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            // Just replaced by a request that arrived a moment earlier (another tab): hand out the replacement again
            if (storedRefreshToken is { IsRevoked: true, ReplacedByToken: not null, RevokedAt: not null }
                && DateTime.UtcNow - storedRefreshToken.RevokedAt.Value < RotationGrace
                && (string.IsNullOrWhiteSpace(userId) || storedRefreshToken.UserId == userId))
            {
                var replacement = await _refreshTokenRepository.GetByTokenAsync(storedRefreshToken.ReplacedByToken);
                var owner = replacement is { IsActive: true } ? await _userRepository.GetByIdAsync(replacement.UserId) : null;
                if (owner is { IsActive: true } && !await IsLockedBySubscriptionAsync(owner))
                {
                    var (access, _) = _jwtTokenService.GenerateAccessToken(owner);
                    return (true, "Token refreshed successfully", new AuthTokenDto
                    {
                        AccessToken = access,
                        RefreshToken = replacement!.Token,
                        ExpiresIn = 900,
                        RefreshTokenExpiresAt = replacement.ExpiresAt,
                        TokenType = "Bearer"
                    });
                }
            }

            // The access token has usually expired by now, so the caller is anonymous; the token itself identifies the user.
            // When a user id is supplied it must still match.
            if (storedRefreshToken == null || !storedRefreshToken.IsActive ||
                (!string.IsNullOrWhiteSpace(userId) && storedRefreshToken.UserId != userId))
                return (false, "Invalid refresh token", null);

            userId = storedRefreshToken.UserId;
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null || !user.IsActive)
                return (false, "User not found or inactive", null);

            if (await IsLockedBySubscriptionAsync(user))
                return (false, SubscriptionEndedMessage, null);

            var (accessToken, jwtId) = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // Revoke old token
            storedRefreshToken.Revoke(newRefreshToken);
            await _refreshTokenRepository.UpdateAsync(storedRefreshToken);

            // Create new token
            var newRefreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshToken,
                JwtId = jwtId,
                ExpiresAt = RefreshLifetimeEnd,
                IpAddress = storedRefreshToken.IpAddress,
                UserAgent = storedRefreshToken.UserAgent
            };

            await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

            var tokens = new AuthTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = 900, // 15 minutes
                RefreshTokenExpiresAt = RefreshLifetimeEnd,
                TokenType = "Bearer"
            };

            return (true, "Token refreshed successfully", tokens);
        }

        public async Task<bool> RevokeTokenAsync(string userId, string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (token == null || token.UserId != userId)
                return false;

            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token);

            return true;
        }

        public async Task<bool> RevokeTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (token == null)
                return false;

            if (!token.IsRevoked)
            {
                token.Revoke();
                await _refreshTokenRepository.UpdateAsync(token);
            }
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

            return (true, "Password reset successfully");
        }
    }
}
