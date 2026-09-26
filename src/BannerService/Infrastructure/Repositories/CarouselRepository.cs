namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Interfaces;
using BannerService.Domain.ValueObjects;
using BannerService.Infrastructure.Data;

public class CarouselRepository : ICarouselRepository
{
    private readonly ApplicationDbContext _context;

    public CarouselRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Carousel> SaveAsync(Carousel carousel, List<Guid> componentIds)
    {
        _context.Carousels.Add(carousel);
        await _context.SaveChangesAsync();

        // Save carousel components
        var carouselComponents = componentIds
            .Select((componentId, index) => new { carouselId = carousel.Id, componentId, index })
            .Select(x => new CarouselComponent
            {
                Id = Guid.NewGuid(),
                CarouselId = x.carouselId,
                ComponentId = x.componentId,
                Order = x.index
            })
            .ToList();

        _context.CarouselComponents.AddRange(carouselComponents);
        await _context.SaveChangesAsync();

        return carousel;
    }

    public async Task<Carousel?> GetByIdAsync(Guid id, Guid shopId)
    {
        return await _context.Carousels
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Carousel>> GetBannerCarouselsAsync(Guid bannerId, Guid shopId)
    {
        return await _context.Carousels
            .Where(c => c.BannerId == bannerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Guid>> GetCarouselComponentIdsAsync(Guid carouselId)
    {
        return await _context.CarouselComponents
            .Where(cc => cc.CarouselId == carouselId)
            .OrderBy(cc => cc.Order)
            .Select(cc => cc.ComponentId)
            .ToListAsync();
    }

    public async Task UpdateAsync(Carousel carousel, List<Guid> componentIds)
    {
        // Remove old components
        var oldComponents = await _context.CarouselComponents
            .Where(cc => cc.CarouselId == carousel.Id)
            .ToListAsync();
        _context.CarouselComponents.RemoveRange(oldComponents);

        // Add new components
        var newComponents = componentIds
            .Select((componentId, index) => new CarouselComponent
            {
                Id = Guid.NewGuid(),
                CarouselId = carousel.Id,
                ComponentId = componentId,
                Order = index
            })
            .ToList();
        _context.CarouselComponents.AddRange(newComponents);

        _context.Carousels.Update(carousel);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, Guid shopId)
    {
        var carousel = await GetByIdAsync(id, shopId);
        if (carousel != null)
        {
            _context.Carousels.Remove(carousel);
            await _context.SaveChangesAsync();
        }
    }
}

public class CarouselComponent
{
    public Guid Id { get; set; }
    public Guid CarouselId { get; set; }
    public Guid ComponentId { get; set; }
    public int Order { get; set; }
}
