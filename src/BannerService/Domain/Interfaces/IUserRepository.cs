namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string userId);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByVerificationTokenAsync(string token);
        Task<User?> GetByPasswordResetTokenAsync(string token);
        Task<List<User>> GetByShopIdAsync(string shopId);
        Task<List<User>> GetAllActiveAsync();
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(string userId);
        Task<bool> ExistsAsync(string email);
        Task<int> GetCountByShopAsync(string shopId);
        Task<(List<User> items, int total)> GetPagedAsync(string? search, string? shopId, bool includeInactive, int page, int pageSize);
        Task<List<User>> SearchAsync(string searchTerm, string shopId);
    }
}
