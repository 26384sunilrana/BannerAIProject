namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IAdminDeletionRequestRepository
    {
        Task<AdminDeletionRequest?> GetByIdAsync(string requestId);
        Task<List<AdminDeletionRequest>> GetPendingAsync();
        Task<List<AdminDeletionRequest>> GetByAdminIdAsync(string adminId);
        Task<AdminDeletionRequest?> GetPendingByAdminIdAsync(string adminId);
        Task<AdminDeletionRequest> CreateAsync(AdminDeletionRequest request);
        Task<AdminDeletionRequest> UpdateAsync(AdminDeletionRequest request);
        Task<bool> DeleteAsync(string requestId);
    }
}
