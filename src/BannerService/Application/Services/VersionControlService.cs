namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;
    using Dto;

public interface IVersionControlService
{
    Task<BannerVersion> CreateSnapshotAsync(Banner banner, string? changeDescription, Guid userId);
    Task<List<BannerVersionDto>> ListVersionsAsync(Guid bannerId, Guid shopId);
    Task<BannerVersionDetailDto?> GetVersionDetailAsync(Guid bannerId, int versionNumber, Guid shopId);
    Task<Banner> RestoreVersionAsync(Guid bannerId, int versionNumber, Guid shopId, Guid userId, string? userName = null);
}

public class VersionControlService : IVersionControlService
{
    private readonly IBannerVersionRepository _versionRepository;
    private readonly IBannerRepository _bannerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublishWorkflowRepository _workflowRepository;

    /// <summary>A banner keeps its newest 10 versions.</summary>
    public const int MaxVersions = 10;

    public VersionControlService(
        IBannerVersionRepository versionRepository,
        IBannerRepository bannerRepository,
        IUnitOfWork unitOfWork,
        IPublishWorkflowRepository workflowRepository)
    {
        _versionRepository = versionRepository;
        _bannerRepository = bannerRepository;
        _unitOfWork = unitOfWork;
        _workflowRepository = workflowRepository;
    }

    public async Task<BannerVersion> CreateSnapshotAsync(Banner banner, string? changeDescription, Guid userId)
    {
        if (banner == null)
            throw new ArgumentNullException(nameof(banner));
        if (!string.IsNullOrEmpty(changeDescription) && changeDescription.Length > 500)
            throw new ArgumentException("Change description must be <= 500 characters");

        var versionNumber = await _versionRepository.GetNextVersionNumberAsync(banner.Id, banner.ShopId);
        var snapshot = new BannerSnapshot(banner);
        var version = new BannerVersion(banner.Id, banner.ShopId, versionNumber, snapshot, userId, changeDescription);

        var saved = await _versionRepository.SaveAsync(version);
        await _versionRepository.DeactivateOldVersionsAsync(banner.Id, banner.ShopId, MaxVersions);
        return saved;
    }

    public async Task<List<BannerVersionDto>> ListVersionsAsync(Guid bannerId, Guid shopId)
    {
        var versions = await _versionRepository.GetVersionsAsync(bannerId, shopId);
        return versions.Select(v => new BannerVersionDto
        {
            VersionNumber = v.VersionNumber,
            CreatedAt = v.CreatedAt,
            CreatedBy = v.CreatedBy,
            ChangeDescription = v.ChangeDescription,
            IsActive = v.IsActive,
            ComponentCount = v.Snapshot.Components.Count,
            Width = v.Snapshot.Width,
            Height = v.Snapshot.Height,
            BannerName = v.Snapshot.Name
        }).ToList();
    }

    public async Task<BannerVersionDetailDto?> GetVersionDetailAsync(Guid bannerId, int versionNumber, Guid shopId)
    {
        var version = await _versionRepository.GetVersionAsync(bannerId, versionNumber, shopId);
        if (version == null)
            return null;

        return new BannerVersionDetailDto
        {
            VersionNumber = version.VersionNumber,
            CreatedAt = version.CreatedAt,
            CreatedBy = version.CreatedBy,
            ChangeDescription = version.ChangeDescription,
            BannerName = version.Snapshot.Name,
            BannerDescription = version.Snapshot.Description,
            Width = version.Snapshot.Width,
            Height = version.Snapshot.Height,
            Components = version.Snapshot.Components.Select(c => new PreviewComponentDto
            {
                ComponentId = c.ComponentId,
                ComponentType = c.ComponentType,
                PositionX = c.PositionX,
                PositionY = c.PositionY,
                SizeWidth = c.SizeWidth,
                SizeHeight = c.SizeHeight,
                ZIndex = c.ZIndex,
                Properties = string.IsNullOrEmpty(c.PropertiesJson)
                    ? new Dictionary<string, object>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(c.PropertiesJson) ?? new()
            }).ToList()
        };
    }

    private async Task SubmitRestoreForApprovalAsync(Banner banner, int versionNumber, Guid userId, string userName)
    {
        var reason = $"Restored to version {versionNumber}";
        var workflow = await _workflowRepository.GetByBannerIdAsync(banner.Id);

        if (workflow == null)
        {
            workflow = new PublishWorkflow { BannerId = banner.Id, ShopId = banner.ShopId, SubmittedByUserId = userId };
            workflow.SubmitForApproval(userId, userName);
            await _workflowRepository.CreateAsync(workflow);
            return;
        }

        workflow.ResubmitForApproval(userId, userName, reason);
        await _workflowRepository.UpdateAsync(workflow);
    }

    public async Task<Banner> RestoreVersionAsync(Guid bannerId, int versionNumber, Guid shopId, Guid userId, string? userName = null)
    {
        if (versionNumber <= 0)
            throw new ArgumentException("Version number must be greater than 0");

        var version = await _versionRepository.GetVersionAsync(bannerId, versionNumber, shopId);
        if (version == null)
            throw new KeyNotFoundException($"Version {versionNumber} not found for banner {bannerId}");

        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new KeyNotFoundException($"Banner {bannerId} not found");

        // Keep the current state as a version before overwriting it, so the latest is never lost
        var latest = (await _versionRepository.GetVersionsAsync(bannerId, shopId)).FirstOrDefault();
        if (latest != null && !latest.Snapshot.ContentEquals(new BannerSnapshot(banner)))
        {
            await _versionRepository.SaveAsync(new BannerVersion(
                bannerId, shopId,
                await _versionRepository.GetNextVersionNumberAsync(bannerId, shopId),
                new BannerSnapshot(banner), userId,
                $"Before restoring version {versionNumber}"));
        }

        // Remove all current components
        foreach (var component in banner.Components.ToList())
            banner.RemoveComponent(component.Id);

        // Restore components from snapshot
        foreach (var componentSnapshot in version.Snapshot.Components)
        {
            var component = new Component(
                banner.Id,
                (ComponentType)componentSnapshot.ComponentType,
                new Position(componentSnapshot.PositionX, componentSnapshot.PositionY),
                new Size(componentSnapshot.SizeWidth, componentSnapshot.SizeHeight),
                componentSnapshot.ZIndex,
                componentSnapshot.PropertiesJson);
            banner.AddComponent(component);
        }

        banner.UpdateName(version.Snapshot.Name);
        banner.UpdateDescription(version.Snapshot.Description);

        // Create restoration snapshot
        var snapshot = new BannerSnapshot(banner);
        var restorationVersion = new BannerVersion(
            bannerId, shopId,
            await _versionRepository.GetNextVersionNumberAsync(bannerId, shopId),
            snapshot, userId,
            $"Restored to version {versionNumber}");

        // A restored version is treated like a new banner: it is taken off air and must be approved again
        banner.Unpublish();

        await _bannerRepository.UpdateAsync(banner);
        await _versionRepository.SaveAsync(restorationVersion);
        await _versionRepository.DeactivateOldVersionsAsync(bannerId, shopId, MaxVersions);
        await _unitOfWork.CommitAsync();

        await SubmitRestoreForApprovalAsync(banner, versionNumber, userId, userName ?? "user");

        return banner;
    }
}
}
