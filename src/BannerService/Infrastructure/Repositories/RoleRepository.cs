namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetByIdAsync(string roleId)
        {
            return await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }

        public async Task<Role?> GetByNameAsync(string roleName)
        {
            return await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive);
        }

        public async Task<List<Role>> GetAllAsync()
        {
            return await _context.Roles
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<List<Role>> GetByUserIdAsync(string userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role!)
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<Role> CreateAsync(Role role)
        {
            role.CreatedAt = DateTime.UtcNow;
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        public async Task<User> AssignRoleAsync(string userId, string roleId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new InvalidOperationException($"User {userId} not found");

            var role = await GetByIdAsync(roleId);
            if (role == null)
                throw new InvalidOperationException($"Role {roleId} not found");

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow
            };

            user.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> RemoveRoleAsync(string userId, string roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
                return false;

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserHasRoleAsync(string userId, string roleName)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId &&
                                ur.Role != null &&
                                ur.Role.Name == roleName &&
                                ur.Role.IsActive);
        }
    }
}
