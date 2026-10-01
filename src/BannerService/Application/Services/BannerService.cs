namespace BannerService.Application.Services;

using System.Text.Json;
using Domain.Entities;
using Domain.Interfaces;
using Domain.Services;
using Domain.ValueObjects;
using Dto;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;

public class BannerService : IBannerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ComponentValidationService _validationService;
    private readonly ILogger<BannerService> _logger;

    public BannerService(IUnitOfWork unitOfWork, ComponentValidationService validationService, ILogger<BannerService> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BannerResponseDto> CreateBannerAsync(Guid shopId, Guid userId, CreateBannerRequestDto request)
    {
        _logger.LogInformation("Creating banner for shop {ShopId}", shopId);

        ValidateCreateRequest(request);

        var banner = new Banner(shopId, userId, request.Name, request.Description, request.Width, request.Height);
        await _unitOfWork.BannerRepository.CreateAsync(banner);

        _logger.LogInformation("Banner {BannerId} created", banner.Id);
        return MapToResponseDto(banner);
    }

    public async Task<BannerResponseDto> GetBannerAsync(Guid bannerId, Guid shopId)
    {
        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new InvalidOperationException($"Banner {bannerId} not found");

        return MapToResponseDto(banner);
    }

    public async Task<List<BannerResponseDto>> ListBannersAsync(Guid shopId, int page = 1, int pageSize = 20)
    {
        var banners = await _unitOfWork.BannerRepository.GetAllByShopAsync(shopId, page, pageSize);
        return banners.Select(MapToResponseDto).ToList();
    }

    public async Task<BannerResponseDto> UpdateBannerAsync(Guid bannerId, Guid shopId, CreateBannerRequestDto request)
    {
        _logger.LogInformation("Updating banner {BannerId}", bannerId);
        ValidateCreateRequest(request);

        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new InvalidOperationException($"Banner {bannerId} not found");

        if (banner.IsPublished)
            throw new InvalidOperationException("Cannot modify published banner");

        banner.Name = request.Name;
        banner.Description = request.Description;
        banner.Width = request.Width;
        banner.Height = request.Height;

        await _unitOfWork.BannerRepository.UpdateAsync(banner);
        _logger.LogInformation("Banner {BannerId} updated", bannerId);
        return MapToResponseDto(banner);
    }

    public async Task<bool> DeleteBannerAsync(Guid bannerId, Guid shopId)
    {
        _logger.LogInformation("Deleting banner {BannerId}", bannerId);

        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner != null && banner.IsPublished)
            throw new InvalidOperationException("Cannot delete published banner");

        var deleted = await _unitOfWork.BannerRepository.DeleteAsync(bannerId, shopId);
        if (deleted)
            _logger.LogInformation("Banner {BannerId} deleted", bannerId);

        return deleted;
    }

    public async Task<ComponentResponseDto> AddComponentAsync(Guid bannerId, Guid shopId, AddComponentRequestDto request)
    {
        _logger.LogInformation("Adding component to banner {BannerId}", bannerId);
        ValidateComponentRequest(request);

        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new InvalidOperationException($"Banner {bannerId} not found");

        request.Properties.Remove("playbackPlan"); // derived for the player, never stored
        var propertiesJson = JsonSerializer.Serialize(request.Properties);
        var position = new Position(request.PositionX, request.PositionY);
        var size = new Size(request.SizeWidth, request.SizeHeight);

        var component = new Component(
            bannerId,
            (ComponentType)request.ComponentType,
            position,
            size,
            request.ZIndex,
            propertiesJson);

        banner.AddComponent(component);
        await _unitOfWork.BannerRepository.UpdateAsync(banner);

        _logger.LogInformation("Component {ComponentId} added to banner {BannerId}", component.Id, bannerId);
        return MapToComponentDto(component);
    }

    public async Task<ComponentResponseDto> UpdateComponentAsync(Guid bannerId, Guid componentId, Guid shopId, AddComponentRequestDto request)
    {
        _logger.LogInformation("Updating component {ComponentId} in banner {BannerId}", componentId, bannerId);
        ValidateComponentRequest(request);

        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new InvalidOperationException($"Banner {bannerId} not found");

        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            throw new InvalidOperationException($"Component {componentId} not found in banner");

        request.Properties.Remove("playbackPlan"); // derived for the player, never stored
        var propertiesJson = JsonSerializer.Serialize(request.Properties);
        var position = new Position(request.PositionX, request.PositionY);
        var size = new Size(request.SizeWidth, request.SizeHeight);

        banner.UpdateComponent(componentId, position, size, request.ZIndex, propertiesJson);
        await _unitOfWork.BannerRepository.UpdateAsync(banner);

        _logger.LogInformation("Component {ComponentId} updated", componentId);
        return MapToComponentDto(component);
    }

    public async Task<bool> RemoveComponentAsync(Guid bannerId, Guid componentId, Guid shopId)
    {
        _logger.LogInformation("Removing component {ComponentId} from banner {BannerId}", componentId, bannerId);

        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new InvalidOperationException($"Banner {bannerId} not found");

        var component = banner.Components.FirstOrDefault(c => c.Id == componentId);
        if (component == null)
            return false;

        banner.RemoveComponent(componentId);
        await _unitOfWork.BannerRepository.UpdateAsync(banner);

        _logger.LogInformation("Component {ComponentId} removed", componentId);
        return true;
    }

    public async Task<PreviewResponseDto> GetPreviewAsync(Guid bannerId, Guid shopId)
    {
        var banner = await _unitOfWork.BannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new InvalidOperationException($"Banner {bannerId} not found");

        var components = banner.Components.OrderBy(c => c.ZIndex).Select(MapToComponentDto).ToList();

        return new PreviewResponseDto
        {
            BannerId = banner.Id,
            ShopId = banner.ShopId,
            Name = banner.Name,
            Width = banner.Width,
            Height = banner.Height,
            IsPublished = banner.IsPublished,
            Components = components
        };
    }

    private void ValidateCreateRequest(CreateBannerRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 255)
            throw new ArgumentException("Name must be 1-255 characters");

        if (request.Width <= 0 || request.Height <= 0)
            throw new ArgumentException("Width and height must be positive");
    }

    private void ValidateComponentRequest(AddComponentRequestDto request)
    {
        _validationService.ValidatePosition(request.PositionX, request.PositionY, int.MaxValue, int.MaxValue);
        _validationService.ValidateSize(request.SizeWidth, request.SizeHeight);
        _validationService.ValidateZIndex(request.ZIndex);

        if ((ComponentType)request.ComponentType == ComponentType.Video)
            _validationService.ValidateVideoPlaylist(request.Properties);
    }

    private BannerResponseDto MapToResponseDto(Banner banner)
    {
        return new BannerResponseDto
        {
            Id = banner.Id,
            ShopId = banner.ShopId,
            Name = banner.Name,
            Description = banner.Description,
            Width = banner.Width,
            Height = banner.Height,
            IsPublished = banner.IsPublished,
            ComponentCount = banner.Components.Count,
            CreatedAt = banner.CreatedAt,
            UpdatedAt = banner.UpdatedAt
        };
    }

    private ComponentResponseDto MapToComponentDto(Component component)
    {
        var properties = JsonSerializer.Deserialize<Dictionary<string, object>>(component.PropertiesJson) ?? new();

        // Give the player a ready-made schedule for video playlists
        if (component.Type == ComponentType.Video)
        {
            var playlist = VideoPlaylist.FromProperties(properties);
            if (playlist != null)
                properties["playbackPlan"] = playlist.BuildPlan();
        }

        return new ComponentResponseDto
        {
            Id = component.Id,
            BannerId = component.BannerId,
            ComponentType = (int)component.Type,
            PositionX = component.PositionX,
            PositionY = component.PositionY,
            SizeWidth = component.SizeWidth,
            SizeHeight = component.SizeHeight,
            ZIndex = component.ZIndex,
            Properties = properties,
            CreatedAt = component.CreatedAt
        };
    }
}
