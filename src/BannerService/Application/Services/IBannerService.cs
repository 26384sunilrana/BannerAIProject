namespace BannerService.Application.Services;

using Dto;

public interface IBannerService
{
    Task<BannerResponseDto> CreateBannerAsync(Guid shopId, Guid userId, CreateBannerRequestDto request);
    Task<BannerResponseDto> GetBannerAsync(Guid bannerId, Guid shopId);
    Task<List<BannerResponseDto>> ListBannersAsync(Guid shopId, int page = 1, int pageSize = 20);
    Task<BannerResponseDto> UpdateBannerAsync(Guid bannerId, Guid shopId, CreateBannerRequestDto request);
    Task<bool> DeleteBannerAsync(Guid bannerId, Guid shopId);
    Task<ComponentResponseDto> AddComponentAsync(Guid bannerId, Guid shopId, AddComponentRequestDto request);
    Task<ComponentResponseDto> UpdateComponentAsync(Guid bannerId, Guid componentId, Guid shopId, AddComponentRequestDto request);
    Task<bool> RemoveComponentAsync(Guid bannerId, Guid componentId, Guid shopId);
    Task<PreviewResponseDto> GetPreviewAsync(Guid bannerId, Guid shopId);
}
