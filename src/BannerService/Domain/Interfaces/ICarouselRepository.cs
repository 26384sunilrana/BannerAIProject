namespace BannerService.Domain.Interfaces;

using Entities;
using ValueObjects;

public interface ICarouselRepository
{
    Task<Carousel> SaveAsync(Carousel carousel, List<Guid> componentIds);
    Task<Carousel?> GetByIdAsync(Guid id, Guid shopId);
    Task<List<Carousel>> GetBannerCarouselsAsync(Guid bannerId, Guid shopId);
    Task UpdateAsync(Carousel carousel, List<Guid> componentIds);
    Task DeleteAsync(Guid id, Guid shopId);
    Task<List<Guid>> GetCarouselComponentIdsAsync(Guid carouselId);
}
