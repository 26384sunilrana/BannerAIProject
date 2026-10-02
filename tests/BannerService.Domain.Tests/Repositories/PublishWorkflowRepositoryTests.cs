namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class PublishWorkflowRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private PublishWorkflowRepository _repository;
    private readonly Guid _testBannerId = Guid.NewGuid();
    private readonly Guid _testShopId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new PublishWorkflowRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var workflow1 = new PublishWorkflow
        {
            Id = Guid.NewGuid(),
            BannerId = _testBannerId,
            ShopId = _testShopId,
            SubmittedByUserId = _testUserId,
            Status = PublishStatus.PendingApproval
        };

        var workflow2 = new PublishWorkflow
        {
            Id = Guid.NewGuid(),
            BannerId = Guid.NewGuid(),
            ShopId = _testShopId,
            SubmittedByUserId = _testUserId,
            Status = PublishStatus.Approved
        };

        _context.Set<PublishWorkflow>().AddRange(workflow1, workflow2);

        var approvalRequest = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            PublishWorkflowId = workflow1.Id,
            ReviewerId = Guid.NewGuid(),
            RequestedAt = DateTime.UtcNow
        };

        _context.Set<ApprovalRequest>().Add(approvalRequest);

        await _context.SaveChangesAsync();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnWorkflow()
    {
        // Arrange
        var workflow = _context.Set<PublishWorkflow>().First();

        // Act
        var result = await _repository.GetByIdAsync(workflow.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(workflow.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByBannerIdAsync Tests

    [Fact]
    public async Task GetByBannerIdAsync_WithValidBannerId_ShouldReturnLatestWorkflow()
    {
        // Act
        var result = await _repository.GetByBannerIdAsync(_testBannerId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_testBannerId, result.BannerId);
    }

    [Fact]
    public async Task GetByBannerIdAsync_WithInvalidBannerId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByBannerIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByShopIdAsync Tests

    [Fact]
    public async Task GetByShopIdAsync_WithValidShopId_ShouldReturnWorkflows()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(_testShopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, w => Assert.Equal(_testShopId, w.ShopId));
    }

    [Fact]
    public async Task GetByShopIdAsync_WithInvalidShopId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByShopIdAsync_ShouldReturnOrderedByCreatedDateDescending()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(_testShopId);

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(w => w.CreatedAt)));
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithValidStatus_ShouldReturnWorkflows()
    {
        // Act
        var results = await _repository.GetByStatusAsync(PublishStatus.PendingApproval);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, w => Assert.Equal(PublishStatus.PendingApproval, w.Status));
    }

    [Fact]
    public async Task GetByStatusAsync_WithStatusHavingNoWorkflows_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetByStatusAsync(PublishStatus.Rejected);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region GetPendingApprovalsAsync Tests

    [Fact]
    public async Task GetPendingApprovalsAsync_ShouldReturnOnlyPendingWorkflows()
    {
        // Act
        var results = await _repository.GetPendingApprovalsAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, w => Assert.Equal(PublishStatus.PendingApproval, w.Status));
    }

    [Fact]
    public async Task GetPendingApprovalsAsync_ShouldOrderBySubmittedAtDescending()
    {
        // Act
        var results = await _repository.GetPendingApprovalsAsync();

        // Assert
        Assert.True(results.SequenceEqual(results.OrderByDescending(w => w.SubmittedAt)));
    }

    #endregion

    #region GetBySubmitterAsync Tests

    [Fact]
    public async Task GetBySubmitterAsync_WithValidUserId_ShouldReturnWorkflows()
    {
        // Act
        var results = await _repository.GetBySubmitterAsync(_testUserId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, w => Assert.Equal(_testUserId, w.SubmittedByUserId));
    }

    [Fact]
    public async Task GetBySubmitterAsync_WithInvalidUserId_ShouldReturnEmpty()
    {
        // Act
        var results = await _repository.GetBySubmitterAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidWorkflow_ShouldAdd()
    {
        // Arrange
        var workflow = new PublishWorkflow
        {
            BannerId = Guid.NewGuid(),
            ShopId = _testShopId,
            SubmittedByUserId = _testUserId,
            Status = PublishStatus.Draft
        };

        // Act
        var result = await _repository.CreateAsync(workflow);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<PublishWorkflow>().FindAsync(workflow.Id);
        Assert.NotNull(saved);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidWorkflow_ShouldUpdate()
    {
        // Arrange
        var workflow = _context.Set<PublishWorkflow>().First();
        workflow.Status = PublishStatus.Approved;

        // Act
        await _repository.UpdateAsync(workflow);

        // Assert
        var updated = await _context.Set<PublishWorkflow>().FindAsync(workflow.Id);
        Assert.Equal(PublishStatus.Approved, updated.Status);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemove()
    {
        // Arrange
        var workflow = _context.Set<PublishWorkflow>().First();

        // Act
        var result = await _repository.DeleteAsync(workflow.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Set<PublishWorkflow>().FindAsync(workflow.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ApprovalRequest Tests

    [Fact]
    public async Task CreateApprovalRequestAsync_WithValidRequest_ShouldAdd()
    {
        // Arrange
        var workflow = _context.Set<PublishWorkflow>().First();
        var request = new ApprovalRequest
        {
            PublishWorkflowId = workflow.Id,
            ReviewerId = Guid.NewGuid()
        };

        // Act
        var result = await _repository.CreateApprovalRequestAsync(request);

        // Assert
        Assert.NotNull(result);
        var saved = await _context.Set<ApprovalRequest>().FindAsync(request.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetApprovalRequestAsync_WithValidId_ShouldReturnRequest()
    {
        // Arrange
        var request = _context.Set<ApprovalRequest>().First();

        // Act
        var result = await _repository.GetApprovalRequestAsync(request.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Id, result.Id);
    }

    [Fact]
    public async Task GetApprovalRequestAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetApprovalRequestAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetApprovalRequestsByWorkflowAsync_WithValidWorkflowId_ShouldReturnRequests()
    {
        // Arrange
        var workflow = _context.Set<PublishWorkflow>().First();

        // Act
        var results = await _repository.GetApprovalRequestsByWorkflowAsync(workflow.Id);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.Equal(workflow.Id, r.PublishWorkflowId));
    }

    [Fact]
    public async Task GetPendingApprovalsForReviewerAsync_WithValidReviewerId_ShouldReturnRequests()
    {
        // Arrange
        var request = _context.Set<ApprovalRequest>().First();

        // Act
        var results = await _repository.GetPendingApprovalsForReviewerAsync(request.ReviewerId);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task UpdateApprovalRequestAsync_WithValidRequest_ShouldUpdate()
    {
        // Arrange
        var request = _context.Set<ApprovalRequest>().First();
        request.DecisionMadeAt = DateTime.UtcNow;

        // Act
        await _repository.UpdateApprovalRequestAsync(request);

        // Assert
        var updated = await _context.Set<ApprovalRequest>().FindAsync(request.Id);
        Assert.False(updated.IsPending);
    }

    #endregion
}
