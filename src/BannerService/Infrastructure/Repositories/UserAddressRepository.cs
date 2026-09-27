using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using BannerService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BannerService.Infrastructure.Repositories
{
    public class UserAddressRepository : IUserAddressRepository
    {
        private readonly ApplicationDbContext _context;

        public UserAddressRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserAddress?> GetByIdAsync(Guid id)
        {
            return await _context.UserAddresses
                .Include(a => a.CountryNav)
                .Include(a => a.StateNav)
                .Include(a => a.DistrictNav)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<UserAddress>> GetByUserIdAsync(string userId)
        {
            return await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .Include(a => a.CountryNav)
                .Include(a => a.StateNav)
                .Include(a => a.DistrictNav)
                .OrderBy(a => a.IsPrimary ? 0 : 1)
                .ThenBy(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserAddress?> GetPrimaryByUserIdAsync(string userId)
        {
            return await _context.UserAddresses
                .Where(a => a.UserId == userId && a.IsPrimary)
                .Include(a => a.CountryNav)
                .Include(a => a.StateNav)
                .Include(a => a.DistrictNav)
                .FirstOrDefaultAsync();
        }

        public async Task<UserAddress> CreateAsync(UserAddress address)
        {
            _context.UserAddresses.Add(address);
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task<UserAddress> UpdateAsync(UserAddress address)
        {
            _context.UserAddresses.Update(address);
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var address = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id);
            if (address == null)
                return false;

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CountByUserAsync(string userId)
        {
            return await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .CountAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.UserAddresses.AnyAsync(a => a.Id == id);
        }
    }
}
