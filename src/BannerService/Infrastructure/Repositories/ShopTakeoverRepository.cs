namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class ShopTakeoverRepository : IShopTakeoverRepository
{
    private readonly ApplicationDbContext _context;

    public ShopTakeoverRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<ShopTakeover?> GetByIdAsync(Guid id) => _context.ShopTakeovers.FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(ShopTakeover takeover)
    {
        _context.ShopTakeovers.Add(takeover);
        await _context.SaveChangesAsync();
    }

    public Task SaveAsync() => _context.SaveChangesAsync();

    public Task<List<ShopTakeover>> OpenForExistingShopAsync(Guid existingShopId) =>
        _context.ShopTakeovers.Where(t => t.ExistingShopId == existingShopId && t.Status == ShopTakeoverStatus.Requested).ToListAsync();

    public Task<List<ShopTakeover>> ListForShopAsync(Guid shopId) =>
        _context.ShopTakeovers.AsNoTracking().Where(t => t.ExistingShopId == shopId || t.NewShopId == shopId)
            .OrderByDescending(t => t.CreatedAt).Take(100).ToListAsync();

    public Task<List<ShopTakeover>> ListAllAsync() =>
        _context.ShopTakeovers.AsNoTracking().OrderByDescending(t => t.CreatedAt).Take(500).ToListAsync();
}
