namespace BannerService.Domain.Entities
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string ShopId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool EmailVerified { get; set; } = false;
        public string? VerificationToken { get; set; }
        public DateTime? VerificationTokenExpires { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpires { get; set; }
        public int LoginAttempts { get; set; } = 0;
        public DateTime? LastLoginAttempt { get; set; }
        public bool IsLockedOut { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();

        public string GetFullName() => $"{FirstName} {LastName}".Trim();

        public bool HasRole(string roleName)
        {
            return UserRoles.Any(ur => ur.Role.Name == roleName);
        }

        public void LockOut()
        {
            IsLockedOut = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UnlockAccount()
        {
            IsLockedOut = false;
            LoginAttempts = 0;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RecordLoginAttempt()
        {
            LastLoginAttempt = DateTime.UtcNow;
            LoginAttempts++;

            if (LoginAttempts >= 5)
            {
                LockOut();
            }

            UpdatedAt = DateTime.UtcNow;
        }

        public void ResetLoginAttempts()
        {
            LoginAttempts = 0;
            LastLoginAttempt = DateTime.UtcNow;
            IsLockedOut = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void VerifyEmail()
        {
            EmailVerified = true;
            VerificationToken = null;
            VerificationTokenExpires = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPasswordResetToken(string token)
        {
            PasswordResetToken = token;
            PasswordResetExpires = DateTime.UtcNow.AddHours(1);
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsPasswordResetTokenValid()
        {
            return !string.IsNullOrEmpty(PasswordResetToken) &&
                   PasswordResetExpires > DateTime.UtcNow;
        }

        public void ClearPasswordResetToken()
        {
            PasswordResetToken = null;
            PasswordResetExpires = null;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
