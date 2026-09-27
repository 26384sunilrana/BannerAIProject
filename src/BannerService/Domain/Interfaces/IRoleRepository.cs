namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(string roleId);
        Task<Role?> GetByNameAsync(string roleName);
        Task<List<Role>> GetAllAsync();
        Task<List<Role>> GetByUserIdAsync(string userId);
        Task<Role> CreateAsync(Role role);
        Task<User> AssignRoleAsync(string userId, string roleId);
        Task<bool> RemoveRoleAsync(string userId, string roleId);
        Task<bool> UserHasRoleAsync(string userId, string roleName);
    }
}
