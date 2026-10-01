namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class BannerRepository : IBannerRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IShopContextAccessor _shopContext;

    public BannerRepository(ApplicationDbContext context, IShopContextAccessor shopContext)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _shopContext = shopContext ?? throw new ArgumentNullException(nameof(shopContext));
    }

    public async Task<Banner> CreateAsync(Banner banner)
    {
        if (banner.ShopId != _shopContext.ShopId)
            throw new UnauthorizedAccessException("Cannot create banner for different shop");

        _context.Banners.Add(banner);
        await _context.SaveChangesAsync();
        return banner;
    }

    public async Task<Banner?> GetByIdAsync(Guid bannerId, Guid shopId)
    {
        if (shopId != _shopContext.ShopId)
            throw new UnauthorizedAccessException("Cannot access banner from different shop");

        return await _context.Banners
            .Include(b => b.Components)
            .FirstOrDefaultAsync(b => b.Id == bannerId && b.ShopId == shopId);
    }

    public async Task<List<Banner>> GetAllByShopAsync(Guid shopId, int page = 1, int pageSize = 20)
    {
        if (shopId != _shopContext.ShopId)
            throw new UnauthorizedAccessException("Cannot access banners from different shop");

        var skip = (page - 1) * pageSize;

        return await _context.Banners
            .Where(b => b.ShopId == shopId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Include(b => b.Components)
            .ToListAsync();
    }

    public async Task<List<Banner>> GetScheduledByShopAsync(Guid shopId)
    {
        if (shopId != _shopContext.ShopId)
            throw new UnauthorizedAccessException("Cannot access banners from different shop");

        return await _context.Banners
            .Where(b => b.ShopId == shopId && b.PublishStartAt != null && b.PublishEndAt != null)
            .OrderBy(b => b.PublishStartAt)
            .ToListAsync();
    }

    public async Task SaveLayerChangesAsync(Banner banner)
    {
        if (banner.ShopId != _shopContext.ShopId)
            throw new UnauthorizedAccessException("Cannot update banner from different shop");

        _context.ChangeTracker.DetectChanges();
        var moved = _context.ChangeTracker.Entries<Component>()
            .Where(e => e.State == EntityState.Modified && e.Property(c => c.ZIndex).IsModified)
            .Select(e => (component: e.Entity, target: e.Entity.ZIndex))
            .ToList();

        if (moved.Count < 2 || !_context.Database.IsRelational())
        {
            await _context.SaveChangesAsync();
            return;
        }

        // Two components trading layers would each collide with the other's old layer. Park every moving component
        // on its own negative layer first, then move them to their targets, all in one transaction.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        for (var i = 0; i < moved.Count; i++)
            moved[i].component.ZIndex = -(i + 1);
        await _context.SaveChangesAsync();

        foreach (var (component, target) in moved)
            component.ZIndex = target;
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public async Task<Banner> UpdateAsync(Banner banner)
    {
        if (banner.ShopId != _shopContext.ShopId)
            throw new UnauthorizedAccessException("Cannot update banner from different shop");

        if (_context.Entry(banner).State == EntityState.Detached)
            _context.Banners.Update(banner);

        // Component ids are assigned when the component is created, so EF cannot tell a new component from an
        // existing one and would UPDATE a row that does not exist. Components missing from the database are inserted.
        var persisted = (await _context.Components
                .AsNoTracking()
                .Where(c => c.BannerId == banner.Id)
                .Select(c => c.Id)
                .ToListAsync())
            .ToHashSet();

        foreach (var component in banner.Components.Where(c => !persisted.Contains(c.Id)))
            _context.Entry(component).State = EntityState.Added;

        await _context.SaveChangesAsync();
        return banner;
    }

    public async Task<bool> DeleteAsync(Guid bannerId, Guid shopId)
    {
        if (shopId != _shopContext.ShopId)
            throw new UnauthorizedAccessException("Cannot delete banner from different shop");

        var banner = await _context.Banners.FirstOrDefaultAsync(b => b.Id == bannerId && b.ShopId == shopId);
        if (banner == null)
            return false;

        _context.Banners.Remove(banner);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid bannerId, Guid shopId)
    {
        if (shopId != _shopContext.ShopId)
            throw new UnauthorizedAccessException("Cannot check banner from different shop");

        return await _context.Banners.AnyAsync(b => b.Id == bannerId && b.ShopId == shopId);
    }
}
