namespace BannerService.Domain.Entities
{
    public class RefreshToken
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string? JwtId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; } = false;
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? ReplacementJwtId { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;

        // Navigation property
        public User? User { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;

        public void Revoke(string? replacedByToken = null)
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            ReplacedByToken = replacedByToken;
        }
    }
}
