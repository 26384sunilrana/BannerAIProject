namespace BannerService.Presentation.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Application.Services;
    using Application.DTOs;
    using Domain.Interfaces;
    using Domain.Entities;
    using Domain.ValueObjects;

    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/publish-workflow")]
    public class PublishWorkflowController : ControllerBase
    {
        private readonly IPublishWorkflowService _workflowService;
        private readonly IPublishWorkflowRepository _repository;
        private readonly ILogger<PublishWorkflowController> _logger;

        public PublishWorkflowController(
            IPublishWorkflowService workflowService,
            IPublishWorkflowRepository repository,
            ILogger<PublishWorkflowController> logger)
        {
            _workflowService = workflowService;
            _repository = repository;
            _logger = logger;
        }

        [HttpPost("initiate")]
        public async Task<IActionResult> InitiateWorkflow([FromBody] SubmitForApprovalDto dto)
        {
            var userId = User.GetUserId();
            var userName = User.GetUserName();
            try
            {
                var workflow = await _workflowService.InitiateWorkflowAsync(dto.BannerId, Guid.NewGuid(), userId, userName);
                return Ok(new { success = true, data = MapToDto(workflow) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating workflow");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{workflowId}/submit")]
        public async Task<IActionResult> SubmitForApproval(Guid workflowId)
        {
            var userId = User.GetUserId();
            var userName = User.GetUserName();
            try
            {
                var workflow = await _workflowService.SubmitForApprovalAsync(workflowId, userId, userName);
                return Ok(new { success = true, data = MapToDto(workflow) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting workflow {WorkflowId}", workflowId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{workflowId}/approve")]
        public async Task<IActionResult> ApproveWorkflow(Guid workflowId, [FromBody] ApproveWorkflowDto dto)
        {
            var userId = User.GetUserId();
            var userName = User.GetUserName();
            try
            {
                var workflow = await _workflowService.ApproveAsync(workflowId, userId, userName, dto.Comment);
                return Ok(new { success = true, data = MapToDto(workflow) });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving workflow {WorkflowId}", workflowId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{workflowId}/reject")]
        public async Task<IActionResult> RejectWorkflow(Guid workflowId, [FromBody] RejectWorkflowDto dto)
        {
            var userId = User.GetUserId();
            var userName = User.GetUserName();
            try
            {
                var workflow = await _workflowService.RejectAsync(workflowId, userId, userName, dto.Reason);
                return Ok(new { success = true, data = MapToDto(workflow) });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting workflow {WorkflowId}", workflowId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{workflowId}/publish")]
        public async Task<IActionResult> PublishWorkflow(Guid workflowId)
        {
            var userId = User.GetUserId();
            var userName = User.GetUserName();
            try
            {
                var workflow = await _workflowService.PublishAsync(workflowId, userId, userName);
                return Ok(new { success = true, data = MapToDto(workflow) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing workflow {WorkflowId}", workflowId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{workflowId}/unpublish")]
        public async Task<IActionResult> UnpublishWorkflow(Guid workflowId)
        {
            var userId = User.GetUserId();
            var userName = User.GetUserName();
            try
            {
                var workflow = await _workflowService.UnpublishAsync(workflowId, userId, userName);
                return Ok(new { success = true, data = MapToDto(workflow) });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpublishing workflow {WorkflowId}", workflowId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{workflowId}")]
        public async Task<IActionResult> GetWorkflow(Guid workflowId)
        {
            try
            {
                var workflow = await _workflowService.GetWorkflowAsync(workflowId);
                if (workflow == null)
                    return NotFound(new { success = false, message = "Workflow not found" });

                var approvals = await _repository.GetApprovalRequestsByWorkflowAsync(workflowId);
                var detail = new PublishWorkflowDetailDto
                {
                    Workflow = MapToDto(workflow),
                    ApprovalRequests = approvals.Select(MapApprovalToDto).ToList(),
                    ApprovedCount = approvals.Count(a => a.IsApproved && a.DecisionMadeAt.HasValue),
                    PendingCount = approvals.Count(a => !a.IsPending),
                    RejectedCount = approvals.Count(a => !a.IsApproved && a.DecisionMadeAt.HasValue)
                };

                return Ok(new { success = true, data = detail });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow {WorkflowId}", workflowId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("banner/{bannerId}")]
        public async Task<IActionResult> GetWorkflowByBanner(Guid bannerId)
        {
            try
            {
                var workflow = await _workflowService.GetWorkflowByBannerAsync(bannerId);
                if (workflow == null)
                    return NotFound(new { success = false, message = "Workflow not found" });

                return Ok(new { success = true, data = MapToDto(workflow) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow for banner {BannerId}", bannerId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("shop/{shopId}")]
        public async Task<IActionResult> GetShopWorkflows(Guid shopId)
        {
            try
            {
                var workflows = await _workflowService.GetShopWorkflowsAsync(shopId);
                var dtos = workflows.Select(MapToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflows for shop {ShopId}", shopId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingWorkflows()
        {
            try
            {
                var workflows = await _workflowService.GetPendingWorkflowsAsync();
                var dtos = workflows.Select(MapToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending workflows");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("{workflowId}/approval-requests")]
        public async Task<IActionResult> RequestApproval(Guid workflowId, [FromBody] RequestApprovalDto dto)
        {
            try
            {
                await _workflowService.RequestApprovalAsync(workflowId, dto.ReviewerId, "Reviewer", dto.ReviewerEmail, dto.DueDate);
                return Ok(new { success = true, message = "Approval requested" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting approval for workflow {WorkflowId}", workflowId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("approval-requests/{requestId}/approve")]
        public async Task<IActionResult> ApproveRequest(Guid requestId, [FromBody] ApproveRequestDto dto)
        {
            var userId = User.GetUserId();
            try
            {
                await _workflowService.ApproveRequestAsync(requestId, userId, dto.Comment);
                return Ok(new { success = true, message = "Request approved" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("approval-requests/{requestId}/reject")]
        public async Task<IActionResult> RejectRequest(Guid requestId, [FromBody] RejectRequestDto dto)
        {
            var userId = User.GetUserId();
            try
            {
                await _workflowService.RejectRequestAsync(requestId, userId, dto.Reason);
                return Ok(new { success = true, message = "Request rejected" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting request {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("reviewer/{reviewerId}/pending")]
        public async Task<IActionResult> GetPendingApprovalsForReviewer(Guid reviewerId)
        {
            try
            {
                var requests = await _workflowService.GetPendingApprovalsForReviewerAsync(reviewerId);
                var dtos = requests.Select(MapApprovalToDto).ToList();
                return Ok(new { success = true, data = dtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending approvals for reviewer {ReviewerId}", reviewerId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #region Mapping Helpers

        private PublishWorkflowDto MapToDto(PublishWorkflow workflow)
        {
            return new PublishWorkflowDto
            {
                Id = workflow.Id,
                BannerId = workflow.BannerId,
                ShopId = workflow.ShopId,
                SubmittedByUserId = workflow.SubmittedByUserId,
                Status = workflow.Status.ToString(),
                SubmittedAt = workflow.SubmittedAt,
                ApprovedAt = workflow.ApprovedAt,
                PublishedAt = workflow.PublishedAt,
                RejectedAt = workflow.RejectedAt,
                RejectionReason = workflow.RejectionReason,
                Events = workflow.Events.Select(MapEventToDto).ToList(),
                CreatedAt = workflow.CreatedAt,
                UpdatedAt = workflow.UpdatedAt
            };
        }

        private PublishEventDto MapEventToDto(PublishEvent evt)
        {
            return new PublishEventDto
            {
                EventType = evt.EventType.ToString(),
                ActorId = evt.ActorId,
                ActorName = evt.ActorName,
                OccurredAt = evt.OccurredAt,
                Comment = evt.Comment,
                Outcome = evt.Outcome,
                Metadata = evt.Metadata
            };
        }

        private ApprovalRequestDto MapApprovalToDto(ApprovalRequest request)
        {
            return new ApprovalRequestDto
            {
                Id = request.Id,
                PublishWorkflowId = request.PublishWorkflowId,
                ReviewerId = request.ReviewerId,
                ReviewerName = request.ReviewerName,
                ReviewerEmail = request.ReviewerEmail,
                IsApproved = request.IsApproved,
                DecisionComment = request.DecisionComment,
                DecisionMadeAt = request.DecisionMadeAt,
                RequestedAt = request.RequestedAt,
                DueDate = request.DueDate,
                IsPending = request.IsPending
            };
        }

        #endregion
    }
}
