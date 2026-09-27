namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;

    public class PublishWorkflowService : IPublishWorkflowService
    {
        private readonly IPublishWorkflowRepository _repository;

        public PublishWorkflowService(IPublishWorkflowRepository repository)
        {
            _repository = repository;
        }

        public async Task<PublishWorkflow> InitiateWorkflowAsync(Guid bannerId, Guid shopId, Guid userId, string userName)
        {
            var workflow = new PublishWorkflow
            {
                BannerId = bannerId,
                ShopId = shopId,
                SubmittedByUserId = userId,
                Status = PublishStatus.Draft
            };

            return await _repository.CreateAsync(workflow);
        }

        public async Task<PublishWorkflow> SubmitForApprovalAsync(Guid workflowId, Guid userId, string userName)
        {
            var workflow = await _repository.GetByIdAsync(workflowId);
            if (workflow == null)
                throw new KeyNotFoundException($"Workflow {workflowId} not found");

            workflow.SubmitForApproval(userId, userName);
            return await _repository.UpdateAsync(workflow);
        }

        public async Task<PublishWorkflow> ApproveAsync(Guid workflowId, Guid reviewerId, string reviewerName, string? comment = null)
        {
            var workflow = await _repository.GetByIdAsync(workflowId);
            if (workflow == null)
                throw new KeyNotFoundException($"Workflow {workflowId} not found");

            if (!workflow.CanBeRejected && workflow.Status != PublishStatus.PendingApproval)
                throw new InvalidOperationException("Workflow is not pending approval");

            workflow.Approve(reviewerId, reviewerName, comment);
            return await _repository.UpdateAsync(workflow);
        }

        public async Task<PublishWorkflow> RejectAsync(Guid workflowId, Guid reviewerId, string reviewerName, string reason)
        {
            var workflow = await _repository.GetByIdAsync(workflowId);
            if (workflow == null)
                throw new KeyNotFoundException($"Workflow {workflowId} not found");

            if (!workflow.CanBeRejected)
                throw new InvalidOperationException("Workflow cannot be rejected in its current state");

            workflow.Reject(reviewerId, reviewerName, reason);
            return await _repository.UpdateAsync(workflow);
        }

        public async Task<PublishWorkflow> PublishAsync(Guid workflowId, Guid userId, string userName)
        {
            var workflow = await _repository.GetByIdAsync(workflowId);
            if (workflow == null)
                throw new KeyNotFoundException($"Workflow {workflowId} not found");

            if (!workflow.CanBePublished)
                throw new InvalidOperationException("Workflow must be approved before publishing");

            workflow.Publish(userId, userName);
            return await _repository.UpdateAsync(workflow);
        }

        public async Task<PublishWorkflow> UnpublishAsync(Guid workflowId, Guid userId, string userName)
        {
            var workflow = await _repository.GetByIdAsync(workflowId);
            if (workflow == null)
                throw new KeyNotFoundException($"Workflow {workflowId} not found");

            if (!workflow.IsPublished)
                throw new InvalidOperationException("Only published workflows can be unpublished");

            workflow.Unpublish(userId, userName);
            return await _repository.UpdateAsync(workflow);
        }

        public async Task<PublishWorkflow?> GetWorkflowAsync(Guid workflowId)
        {
            return await _repository.GetByIdAsync(workflowId);
        }

        public async Task<PublishWorkflow?> GetWorkflowByBannerAsync(Guid bannerId)
        {
            return await _repository.GetByBannerIdAsync(bannerId);
        }

        public async Task<List<PublishWorkflow>> GetShopWorkflowsAsync(Guid shopId)
        {
            return await _repository.GetByShopIdAsync(shopId);
        }

        public async Task<List<PublishWorkflow>> GetPendingWorkflowsAsync()
        {
            return await _repository.GetPendingApprovalsAsync();
        }

        public async Task<List<ApprovalRequest>> GetPendingApprovalsForReviewerAsync(Guid reviewerId)
        {
            return await _repository.GetPendingApprovalsForReviewerAsync(reviewerId);
        }

        public async Task RequestApprovalAsync(Guid workflowId, Guid reviewerId, string reviewerName, string reviewerEmail, DateTime? dueDate = null)
        {
            var workflow = await _repository.GetByIdAsync(workflowId);
            if (workflow == null)
                throw new KeyNotFoundException($"Workflow {workflowId} not found");

            var request = new ApprovalRequest
            {
                PublishWorkflowId = workflowId,
                ReviewerId = reviewerId,
                ReviewerName = reviewerName,
                ReviewerEmail = reviewerEmail,
                DueDate = dueDate,
                RequestedAt = DateTime.UtcNow
            };

            await _repository.CreateApprovalRequestAsync(request);

            workflow.Events.Add(new PublishEvent
            {
                EventType = PublishEventType.ApprovalRequested,
                ActorId = workflow.SubmittedByUserId,
                ActorName = "System",
                OccurredAt = DateTime.UtcNow,
                Comment = $"Approval requested from {reviewerName}",
                Outcome = "Requested"
            });

            await _repository.UpdateAsync(workflow);
        }

        public async Task ApproveRequestAsync(Guid requestId, Guid reviewerId, string? comment = null)
        {
            var request = await _repository.GetApprovalRequestAsync(requestId);
            if (request == null)
                throw new KeyNotFoundException($"Approval request {requestId} not found");

            request.Approve(comment);
            await _repository.UpdateApprovalRequestAsync(request);

            var workflow = await _repository.GetByIdAsync(request.PublishWorkflowId);
            if (workflow != null)
            {
                workflow.Events.Add(new PublishEvent
                {
                    EventType = PublishEventType.ApprovedByReviewer,
                    ActorId = reviewerId,
                    ActorName = request.ReviewerName,
                    OccurredAt = DateTime.UtcNow,
                    Comment = comment,
                    Outcome = "Approved"
                });

                if (await AreAllApprovalsCompletedAsync(request.PublishWorkflowId))
                {
                    workflow.Approve(reviewerId, request.ReviewerName, comment);
                }

                await _repository.UpdateAsync(workflow);
            }
        }

        public async Task RejectRequestAsync(Guid requestId, Guid reviewerId, string reason)
        {
            var request = await _repository.GetApprovalRequestAsync(requestId);
            if (request == null)
                throw new KeyNotFoundException($"Approval request {requestId} not found");

            request.Reject(reason);
            await _repository.UpdateApprovalRequestAsync(request);

            var workflow = await _repository.GetByIdAsync(request.PublishWorkflowId);
            if (workflow != null)
            {
                workflow.Reject(reviewerId, request.ReviewerName, reason);
                await _repository.UpdateAsync(workflow);
            }
        }

        private async Task<bool> AreAllApprovalsCompletedAsync(Guid workflowId)
        {
            var requests = await _repository.GetApprovalRequestsByWorkflowAsync(workflowId);
            return requests.All(r => r.DecisionMadeAt.HasValue);
        }
    }
}
