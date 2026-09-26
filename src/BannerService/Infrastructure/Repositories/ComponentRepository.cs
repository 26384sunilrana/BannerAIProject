namespace BannerService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Interfaces;
using Data;

public class ComponentRepository : IComponentRepository
{
    private readonly ApplicationDbContext _context;

    public ComponentRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Component> CreateAsync(Component component)
    {
        _context.Components.Add(component);
        await _context.SaveChangesAsync();
        return component;
    }

    public async Task<Component?> GetByIdAsync(Guid componentId)
    {
        return await _context.Components.FirstOrDefaultAsync(c => c.Id == componentId);
    }

    public async Task<List<Component>> GetByBannerIdAsync(Guid bannerId)
    {
        return await _context.Components
            .Where(c => c.BannerId == bannerId)
            .OrderBy(c => c.ZIndex)
            .ToListAsync();
    }

    public async Task<Component> UpdateAsync(Component component)
    {
        _context.Components.Update(component);
        await _context.SaveChangesAsync();
        return component;
    }

    public async Task<bool> DeleteAsync(Guid componentId)
    {
        var component = await _context.Components.FirstOrDefaultAsync(c => c.Id == componentId);
        if (component == null)
            return false;

        _context.Components.Remove(component);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByZIndexAsync(Guid bannerId, int zIndex)
    {
        return await _context.Components.AnyAsync(c => c.BannerId == bannerId && c.ZIndex == zIndex);
    }
}
