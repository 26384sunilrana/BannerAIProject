namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<RefreshToken?> GetByIdAsync(string id)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Id == id);
        }

        public async Task<List<RefreshToken>> GetByUserIdAsync(string userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<RefreshToken>> GetActiveByUserIdAsync(string userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }

        public async Task<RefreshToken> AddAsync(RefreshToken refreshToken)
        {
            refreshToken.Id = Guid.NewGuid().ToString();
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task<RefreshToken> UpdateAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var token = await GetByIdAsync(id);
            if (token == null)
                return false;

            _context.RefreshTokens.Remove(token);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
                .ToListAsync();

            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            return expiredTokens.Count;
        }
    }
}
