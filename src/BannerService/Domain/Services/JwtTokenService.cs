namespace BannerService.Domain.Services
{
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using Entities;
    using Microsoft.IdentityModel.Tokens;

    public interface IJwtTokenService
    {
        (string accessToken, string jwtId) GenerateAccessToken(User user);
        string GenerateRefreshToken();

        /// <summary>An hour-long token for a paired screen: the role "Screen" and the shop, and nothing of a person.</summary>
        string GenerateScreenToken(Guid screenId, Guid shopId, string name);

        /// <summary>A short-lived proof that the password was right, to be traded for a session together with the one-time code.</summary>
        string GenerateTwoFactorChallenge(string userId);

        /// <summary>The user id inside a valid, unexpired challenge, or null.</summary>
        string? ReadTwoFactorChallenge(string challenge);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        bool ValidateToken(string token, out ClaimsPrincipal? principal);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly SigningCredentials _signingCredentials;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;

            var key = Encoding.ASCII.GetBytes(
                _configuration["Jwt:SecretKey"] ?? _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")
            );

            _signingCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            );
        }

        public (string accessToken, string jwtId) GenerateAccessToken(User user)
        {
            var jwtId = Guid.NewGuid().ToString();
            var roles = user.UserRoles.Select(ur => ur.Role?.Name).Where(r => r != null).ToList();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.GetFullName()),
                new Claim("ShopId", user.ShopId),
                new Claim("shop_id", user.ShopId),
                new Claim("JwtId", jwtId)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role ?? string.Empty));
            }

            // An administrator without two-step sign-in, where it is required, may only reach the screens that set it up
            if (roles.Contains(Role.Admin) && !user.TwoFactorEnabled && _configuration.GetValue("Security:RequireTwoFactorForAdmins", false))
                claims.Add(new Claim(TwoFactorSetupClaim, "true"));

            var expiresIn = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? _configuration["Jwt:ExpiresInMinutes"] ?? "15");

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresIn),
                signingCredentials: _signingCredentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            var accessToken = tokenHandler.WriteToken(token);

            return (accessToken, jwtId);
        }

        public string GenerateScreenToken(Guid screenId, Guid shopId, string name)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, screenId.ToString()),
                new Claim(ClaimTypes.Name, name),
                new Claim("shop_id", shopId.ToString()),
                new Claim("ShopId", shopId.ToString()),
                new Claim(ClaimTypes.Role, "Screen"),
            };
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: _signingCredentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public const string TwoFactorSetupClaim = "two_factor_setup_required";
        private const string ChallengeAudience = "BannerAI.TwoFactorChallenge";

        public string GenerateTwoFactorChallenge(string userId)
        {
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: ChallengeAudience, // not the API's audience: it cannot be used as an access token
                claims: new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: _signingCredentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string? ReadTwoFactorChallenge(string challenge)
        {
            try
            {
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"] ?? _configuration["Jwt:Key"] ?? string.Empty);
                var principal = new JwtSecurityTokenHandler().ValidateToken(challenge, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = ChallengeAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                }, out _);
                return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            catch
            {
                return null;
            }
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(
                            _configuration["Jwt:SecretKey"] ?? _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")
                        )
                    ),
                    ValidateLifetime = false
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                    !jwtSecurityToken.Header.Alg.Equals(
                        SecurityAlgorithms.HmacSha256Signature,
                        StringComparison.InvariantCultureIgnoreCase
                    ))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public bool ValidateToken(string token, out ClaimsPrincipal? principal)
        {
            principal = null;

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(
                            _configuration["Jwt:SecretKey"] ?? _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")
                        )
                    ),
                    ValidateLifetime = true
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
