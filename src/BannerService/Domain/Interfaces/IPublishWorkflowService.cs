namespace BannerService.Domain.Interfaces
{
    using Domain.Entities;

    public interface IPublishWorkflowService
    {
        Task<PublishWorkflow> InitiateWorkflowAsync(Guid bannerId, Guid shopId, Guid userId, string userName);
        Task<PublishWorkflow> SubmitForApprovalAsync(Guid workflowId, Guid userId, string userName);
        Task<PublishWorkflow> ApproveAsync(Guid workflowId, Guid reviewerId, string reviewerName, string? comment = null);
        Task<PublishWorkflow> RejectAsync(Guid workflowId, Guid reviewerId, string reviewerName, string reason);
        Task<PublishWorkflow> PublishAsync(Guid workflowId, Guid userId, string userName);
        Task<PublishWorkflow> UnpublishAsync(Guid workflowId, Guid userId, string userName);
        Task<PublishWorkflow?> GetWorkflowAsync(Guid workflowId);
        Task<PublishWorkflow?> GetWorkflowByBannerAsync(Guid bannerId);
        Task<List<PublishWorkflow>> GetShopWorkflowsAsync(Guid shopId);
        Task<List<PublishWorkflow>> GetPendingWorkflowsAsync();
        Task<List<ApprovalRequest>> GetPendingApprovalsForReviewerAsync(Guid reviewerId);
        Task RequestApprovalAsync(Guid workflowId, Guid reviewerId, string reviewerName, string reviewerEmail, DateTime? dueDate = null);
        Task ApproveRequestAsync(Guid requestId, Guid reviewerId, string? comment = null);
        Task RejectRequestAsync(Guid requestId, Guid reviewerId, string reason);
    }
}
