namespace BannerService.Domain.Interfaces
{
    using Domain.Entities;

    public interface IPublishWorkflowRepository
    {
        Task<PublishWorkflow?> GetByIdAsync(Guid workflowId);
        Task<PublishWorkflow?> GetByBannerIdAsync(Guid bannerId);
        Task<List<PublishWorkflow>> GetByShopIdAsync(Guid shopId);
        Task<List<PublishWorkflow>> GetByStatusAsync(PublishStatus status);
        Task<List<PublishWorkflow>> GetPendingApprovalsAsync();
        Task<List<PublishWorkflow>> GetBySubmitterAsync(Guid userId);
        Task<PublishWorkflow> CreateAsync(PublishWorkflow workflow);
        Task<PublishWorkflow> UpdateAsync(PublishWorkflow workflow);
        Task<bool> DeleteAsync(Guid workflowId);
        Task<ApprovalRequest> CreateApprovalRequestAsync(ApprovalRequest request);
        Task<ApprovalRequest?> GetApprovalRequestAsync(Guid requestId);
        Task<List<ApprovalRequest>> GetApprovalRequestsByWorkflowAsync(Guid workflowId);
        Task<List<ApprovalRequest>> GetPendingApprovalsForReviewerAsync(Guid reviewerId);
        Task<ApprovalRequest> UpdateApprovalRequestAsync(ApprovalRequest request);
    }
}
