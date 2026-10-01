using BannerService.Application.Services;
using BannerService.Domain.Entities;
using BannerService.Domain.Interfaces;
using Moq;
using Xunit;

namespace BannerService.Application.Tests.Services;

public class PublishWorkflowApproverTests
{
    private readonly Mock<IPublishWorkflowRepository> _workflows = new();
    private readonly Mock<IShopRepository> _shops = new();
    private readonly PublishWorkflowService _service;

    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _executiveId = Guid.NewGuid();
    private readonly Shop _shop;
    private readonly PublishWorkflow _workflow;

    public PublishWorkflowApproverTests()
    {
        _shop = new Shop { Id = Guid.NewGuid(), Name = "Shop", OwnerUserId = _ownerId };
        _workflow = new PublishWorkflow { ShopId = _shop.Id, BannerId = Guid.NewGuid(), Status = PublishStatus.PendingApproval };

        _shops.Setup(r => r.GetByIdAsync(_shop.Id)).ReturnsAsync(_shop);
        _workflows.Setup(r => r.GetByIdAsync(_workflow.Id)).ReturnsAsync(_workflow);
        _workflows.Setup(r => r.UpdateAsync(It.IsAny<PublishWorkflow>())).ReturnsAsync((PublishWorkflow w) => w);

        _service = new PublishWorkflowService(_workflows.Object, _shops.Object);
    }

    [Fact]
    public async Task Approve_ByOwner_WhenOwnerIsApprover_Succeeds()
    {
        var result = await _service.ApproveAsync(_workflow.Id, _ownerId, "Owner");

        Assert.Equal(PublishStatus.Approved, result.Status);
    }

    [Fact]
    public async Task Approve_ByExecutiveWhoIsNotAnApprover_IsForbidden()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ApproveAsync(_workflow.Id, _executiveId, "Exec"));
        Assert.Equal(PublishStatus.PendingApproval, _workflow.Status);
    }

    [Fact]
    public async Task Approve_ByDesignatedExecutive_Succeeds()
    {
        _shop.SetApprovers(true, new[] { _executiveId.ToString() });

        var result = await _service.ApproveAsync(_workflow.Id, _executiveId, "Exec");

        Assert.Equal(PublishStatus.Approved, result.Status);
    }

    [Fact]
    public async Task Approve_ByOwner_WhenOnlyExecutiveApproves_IsForbidden()
    {
        _shop.SetApprovers(false, new[] { _executiveId.ToString() });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.ApproveAsync(_workflow.Id, _ownerId, "Owner"));
    }

    [Fact]
    public async Task Reject_ByNonApprover_IsForbidden()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _service.RejectAsync(_workflow.Id, _executiveId, "Exec", "no"));
    }
}
