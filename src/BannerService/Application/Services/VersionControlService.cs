<<<<<<< HEAD
namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;
    using DTOs;
=======
namespace BannerService.Application.Services;

using global::BannerService.Domain.Entities;
using global::BannerService.Domain.Interfaces;
using global::BannerService.Domain.ValueObjects;
using global::BannerService.Application.Dto;
>>>>>>> fdd9d7e4866c32af53c9f511abe3780d5ff25658

public interface IVersionControlService
{
    Task<BannerVersion> CreateSnapshotAsync(Banner banner, string? changeDescription, Guid userId);
    Task<List<BannerVersionDto>> ListVersionsAsync(Guid bannerId, Guid shopId);
    Task<BannerVersionDetailDto?> GetVersionDetailAsync(Guid bannerId, int versionNumber, Guid shopId);
    Task<Banner> RestoreVersionAsync(Guid bannerId, int versionNumber, Guid shopId, Guid userId);
}

public class VersionControlService : IVersionControlService
{
    private readonly IBannerVersionRepository _versionRepository;
    private readonly IBannerRepository _bannerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VersionControlService(
        IBannerVersionRepository versionRepository,
        IBannerRepository bannerRepository,
        IUnitOfWork unitOfWork)
    {
        _versionRepository = versionRepository;
        _bannerRepository = bannerRepository;
        _unitOfWork = unitOfWork;
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

        return await _versionRepository.SaveAsync(version);
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

    public async Task<Banner> RestoreVersionAsync(Guid bannerId, int versionNumber, Guid shopId, Guid userId)
    {
        if (versionNumber <= 0)
            throw new ArgumentException("Version number must be greater than 0");

        var version = await _versionRepository.GetVersionAsync(bannerId, versionNumber, shopId);
        if (version == null)
            throw new KeyNotFoundException($"Version {versionNumber} not found for banner {bannerId}");

        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new KeyNotFoundException($"Banner {bannerId} not found");

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

        await _bannerRepository.UpdateAsync(banner);
        await _versionRepository.SaveAsync(restorationVersion);
        await _unitOfWork.CommitAsync();

        return banner;
    }
}
}
