<<<<<<< HEAD
namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.Services;
    using Domain.ValueObjects;
    using DTOs;
=======
namespace BannerService.Application.Services;

using global::BannerService.Domain.Entities;
using global::BannerService.Domain.Interfaces;
using global::BannerService.Domain.Services;
using global::BannerService.Domain.ValueObjects;
using global::BannerService.Application.Dto;
>>>>>>> fdd9d7e4866c32af53c9f511abe3780d5ff25658

public interface ICarouselService
{
    Task<CarouselDto> CreateCarouselAsync(Guid bannerId, Guid shopId, int intervalMs,
        int transitionDuration, int transitionType, List<Guid> componentIds);
    Task<CarouselDto?> GetCarouselAsync(Guid carouselId, Guid shopId);
    Task<List<CarouselDto>> GetBannerCarouselsAsync(Guid bannerId, Guid shopId);
    Task<CarouselDto> UpdateCarouselAsync(Guid carouselId, Guid shopId,
        int intervalMs, int transitionDuration, int transitionType, List<Guid> componentIds);
    Task DeleteCarouselAsync(Guid carouselId, Guid shopId);
}

public class CarouselService : ICarouselService
{
    private readonly IBannerRepository _bannerRepository;
    private readonly ICarouselRepository _carouselRepository;
    private readonly EffectValidator _validator;
    private readonly IUnitOfWork _unitOfWork;

    public CarouselService(
        IBannerRepository bannerRepository,
        ICarouselRepository carouselRepository,
        EffectValidator validator,
        IUnitOfWork unitOfWork)
    {
        _bannerRepository = bannerRepository;
        _carouselRepository = carouselRepository;
        _validator = validator;
        _unitOfWork = unitOfWork;
    }

    public async Task<CarouselDto> CreateCarouselAsync(
        Guid bannerId, Guid shopId, int intervalMs, int transitionDuration,
        int transitionType, List<Guid> componentIds)
    {
        var banner = await _bannerRepository.GetByIdAsync(bannerId, shopId);
        if (banner == null)
            throw new KeyNotFoundException("Banner not found");

        var carousel = new Carousel(bannerId, intervalMs, transitionDuration, transitionType, componentIds);
        var validationResult = _validator.ValidateCarouselConfig(carousel, banner);

        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.Error);

        await _carouselRepository.SaveAsync(carousel, componentIds);
        await _unitOfWork.CommitAsync();

        return MapToDto(carousel, componentIds);
    }

    public async Task<CarouselDto?> GetCarouselAsync(Guid carouselId, Guid shopId)
    {
        var carousel = await _carouselRepository.GetByIdAsync(carouselId, shopId);
        if (carousel == null)
            return null;

        var componentIds = await _carouselRepository.GetCarouselComponentIdsAsync(carouselId);
        return MapToDto(carousel, componentIds);
    }

    public async Task<List<CarouselDto>> GetBannerCarouselsAsync(Guid bannerId, Guid shopId)
    {
        var carousels = await _carouselRepository.GetBannerCarouselsAsync(bannerId, shopId);
        var dtos = new List<CarouselDto>();

        foreach (var carousel in carousels)
        {
            var componentIds = await _carouselRepository.GetCarouselComponentIdsAsync(carousel.Id);
            dtos.Add(MapToDto(carousel, componentIds));
        }

        return dtos;
    }

    public async Task<CarouselDto> UpdateCarouselAsync(
        Guid carouselId, Guid shopId, int intervalMs, int transitionDuration,
        int transitionType, List<Guid> componentIds)
    {
        var carousel = await _carouselRepository.GetByIdAsync(carouselId, shopId);
        if (carousel == null)
            throw new KeyNotFoundException("Carousel not found");

        var banner = await _bannerRepository.GetByIdAsync(carousel.BannerId, shopId);
        if (banner == null)
            throw new KeyNotFoundException("Banner not found");

        var updatedCarousel = new Carousel(carousel.BannerId, intervalMs, transitionDuration, transitionType, componentIds);
        var validationResult = _validator.ValidateCarouselConfig(updatedCarousel, banner);

        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.Error);

        await _carouselRepository.UpdateAsync(updatedCarousel, componentIds);
        await _unitOfWork.CommitAsync();

        return MapToDto(updatedCarousel, componentIds);
    }

    public async Task DeleteCarouselAsync(Guid carouselId, Guid shopId)
    {
        await _carouselRepository.DeleteAsync(carouselId, shopId);
        await _unitOfWork.CommitAsync();
    }

    private CarouselDto MapToDto(Carousel carousel, List<Guid> componentIds) => new()
    {
        Id = carousel.Id,
        IntervalMs = carousel.IntervalMs,
        TransitionDuration = carousel.TransitionDuration,
        TransitionType = carousel.TransitionType,
        IsAutoplay = carousel.IsAutoplay,
        Loop = carousel.Loop,
        ComponentIds = componentIds,
        CreatedAt = carousel.CreatedAt
    };
}
}
