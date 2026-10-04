namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class AdminDeletionRequestRepository : IAdminDeletionRequestRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminDeletionRequestRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDeletionRequest?> GetByIdAsync(string requestId)
        {
            return await _context.AdminDeletionRequests
                .Include(r => r.AdminToDelete)
                .Include(r => r.RequestedByAdmin)
                .Include(r => r.ApprovedByAdmin)
                .FirstOrDefaultAsync(r => r.Id == requestId);
        }

        public async Task<List<AdminDeletionRequest>> GetPendingAsync()
        {
            return await _context.AdminDeletionRequests
                .Include(r => r.AdminToDelete)
                .Include(r => r.RequestedByAdmin)
                .Where(r => r.Status == AdminDeletionStatus.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AdminDeletionRequest>> GetByAdminIdAsync(string adminId)
        {
            return await _context.AdminDeletionRequests
                .Include(r => r.AdminToDelete)
                .Include(r => r.RequestedByAdmin)
                .Include(r => r.ApprovedByAdmin)
                .Where(r => r.AdminIdToDelete == adminId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<AdminDeletionRequest?> GetPendingByAdminIdAsync(string adminId)
        {
            return await _context.AdminDeletionRequests
                .Include(r => r.AdminToDelete)
                .Include(r => r.RequestedByAdmin)
                .Where(r => r.AdminIdToDelete == adminId && r.Status == AdminDeletionStatus.Pending)
                .FirstOrDefaultAsync();
        }

        public async Task<AdminDeletionRequest> CreateAsync(AdminDeletionRequest request)
        {
            _context.AdminDeletionRequests.Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<AdminDeletionRequest> UpdateAsync(AdminDeletionRequest request)
        {
            _context.AdminDeletionRequests.Update(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<bool> DeleteAsync(string requestId)
        {
            var request = await _context.AdminDeletionRequests.FindAsync(requestId);
            if (request == null)
                return false;

            _context.AdminDeletionRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
