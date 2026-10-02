namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class PublishWorkflowRepository : IPublishWorkflowRepository
    {
        private readonly ApplicationDbContext _context;

        public PublishWorkflowRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PublishWorkflow?> GetByIdAsync(Guid workflowId)
        {
            return await _context.Set<PublishWorkflow>()
                .FirstOrDefaultAsync(w => w.Id == workflowId);
        }

        public async Task<PublishWorkflow?> GetByBannerIdAsync(Guid bannerId)
        {
            return await _context.Set<PublishWorkflow>()
                .Where(w => w.BannerId == bannerId)
                .OrderByDescending(w => w.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<PublishWorkflow>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.Set<PublishWorkflow>()
                .Where(w => w.ShopId == shopId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PublishWorkflow>> GetByStatusAsync(PublishStatus status)
        {
            return await _context.Set<PublishWorkflow>()
                .Where(w => w.Status == status)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PublishWorkflow>> GetPendingApprovalsAsync()
        {
            return await _context.Set<PublishWorkflow>()
                .Where(w => w.Status == PublishStatus.PendingApproval)
                .OrderByDescending(w => w.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<PublishWorkflow>> GetBySubmitterAsync(Guid userId)
        {
            return await _context.Set<PublishWorkflow>()
                .Where(w => w.SubmittedByUserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<PublishWorkflow> CreateAsync(PublishWorkflow workflow)
        {
            _context.Set<PublishWorkflow>().Add(workflow);
            await _context.SaveChangesAsync();
            return workflow;
        }

        public async Task<PublishWorkflow> UpdateAsync(PublishWorkflow workflow)
        {
            _context.Set<PublishWorkflow>().Update(workflow);
            await _context.SaveChangesAsync();
            return workflow;
        }

        public async Task<bool> DeleteAsync(Guid workflowId)
        {
            var workflow = await GetByIdAsync(workflowId);
            if (workflow == null)
                return false;

            _context.Set<PublishWorkflow>().Remove(workflow);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ApprovalRequest> CreateApprovalRequestAsync(ApprovalRequest request)
        {
            _context.Set<ApprovalRequest>().Add(request);
            await _context.SaveChangesAsync();
            return request;
        }

        public async Task<ApprovalRequest?> GetApprovalRequestAsync(Guid requestId)
        {
            return await _context.Set<ApprovalRequest>()
                .FirstOrDefaultAsync(r => r.Id == requestId);
        }

        public async Task<List<ApprovalRequest>> GetApprovalRequestsByWorkflowAsync(Guid workflowId)
        {
            return await _context.Set<ApprovalRequest>()
                .Where(r => r.PublishWorkflowId == workflowId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<ApprovalRequest>> GetPendingApprovalsForReviewerAsync(Guid reviewerId)
        {
            return await _context.Set<ApprovalRequest>()
                .Where(r => r.ReviewerId == reviewerId && r.DecisionMadeAt == null)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<ApprovalRequest> UpdateApprovalRequestAsync(ApprovalRequest request)
        {
            _context.Set<ApprovalRequest>().Update(request);
            await _context.SaveChangesAsync();
            return request;
        }
    }
}
