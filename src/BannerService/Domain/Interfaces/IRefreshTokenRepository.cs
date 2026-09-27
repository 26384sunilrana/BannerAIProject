namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<RefreshToken?> GetByIdAsync(string id);
        Task<List<RefreshToken>> GetByUserIdAsync(string userId);
        Task<List<RefreshToken>> GetActiveByUserIdAsync(string userId);
        Task<RefreshToken> AddAsync(RefreshToken refreshToken);
        Task<RefreshToken> UpdateAsync(RefreshToken refreshToken);
        Task<bool> DeleteAsync(string id);
        Task<int> DeleteExpiredTokensAsync();
    }
}
