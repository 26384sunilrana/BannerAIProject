namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(string userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());
        }

        public async Task<User?> GetByVerificationTokenAsync(string token)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.VerificationToken == token);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string token)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
        }

        public async Task<List<User>> GetByShopIdAsync(string shopId)
        {
            return await _context.Users
                .Where(u => u.ShopId == shopId && u.IsActive)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllActiveAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(string userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email.ToLower());
        }

        public async Task<int> GetCountByShopAsync(string shopId)
        {
            return await _context.Users.CountAsync(u => u.ShopId == shopId && u.IsActive);
        }

        public async Task<List<User>> SearchAsync(string searchTerm, string shopId)
        {
            var term = searchTerm.ToLower();
            return await _context.Users
                .Where(u => u.ShopId == shopId && u.IsActive &&
                    (u.Email.Contains(term) ||
                     u.FirstName.Contains(term) ||
                     u.LastName.Contains(term) ||
                     u.PhoneNumber != null && u.PhoneNumber.Contains(term)))
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();
        }
    }
}
