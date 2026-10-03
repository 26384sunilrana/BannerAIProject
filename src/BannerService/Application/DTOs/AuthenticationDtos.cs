namespace BannerService.Application.DTOs
{
    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        /// <summary>Not accepted on sign-up: logins for an existing shop are created by its owner.</summary>
        public string? ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }

    public class TwoFactorLoginDto
    {
        public string Challenge { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class RefreshTokenDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AuthTokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; } // in seconds
        /// <summary>When the refresh token stops working; used for the cookie lifetime.</summary>
        public DateTime RefreshTokenExpiresAt { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ShopId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool EmailVerified { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class VerifyEmailDto
    {
        public string Token { get; set; } = string.Empty;
    }

    public class RequestPasswordResetDto
    {
        public string Email { get; set; } = string.Empty;
    }
}
