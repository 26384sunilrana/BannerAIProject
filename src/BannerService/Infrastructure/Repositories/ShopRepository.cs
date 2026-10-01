namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class ShopRepository : IShopRepository
    {
        private readonly ApplicationDbContext _context;

        public ShopRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Shop?> GetByIdAsync(Guid shopId)
        {
            return await _context.Shops
                .Include(s => s.ChildShops)
                .FirstOrDefaultAsync(s => s.Id == shopId);
        }

        public async Task<Shop?> GetByNameAsync(string name)
        {
            return await _context.Shops
                .FirstOrDefaultAsync(s => s.Name == name && s.Status != ShopStatus.Archived);
        }

        public async Task<List<Shop>> GetAllAsync()
        {
            return await _context.Shops
                .Where(s => s.Status != ShopStatus.Archived)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<Shop>> GetActiveAsync()
        {
            return await _context.Shops
                .Where(s => s.Status == ShopStatus.Active)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<Shop>> GetByOwnerAsync(Guid ownerId)
        {
            return await _context.Shops
                .Where(s => s.OwnerUserId == ownerId && s.Status != ShopStatus.Archived)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<Shop>> GetByParentAsync(Guid parentShopId)
        {
            return await _context.Shops
                .Where(s => s.ParentShopId == parentShopId && s.Status != ShopStatus.Archived)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Shop?> GetHierarchyAsync(Guid shopId, int maxDepth = 10)
        {
            var shop = await GetByIdAsync(shopId);
            if (shop == null)
                return null;

            await LoadChildrenRecursively(shop, maxDepth);
            return shop;
        }

        public async Task<List<Shop>> GetDescendantsAsync(Guid shopId, int maxDepth = 10)
        {
            var descendants = new List<Shop>();
            var shop = await GetByIdAsync(shopId);

            if (shop == null)
                return descendants;

            await CollectDescendantsRecursively(shop, descendants, maxDepth);
            return descendants;
        }

        public async Task<List<Shop>> GetAncestorsAsync(Guid shopId)
        {
            var ancestors = new List<Shop>();
            var shop = await GetByIdAsync(shopId);

            if (shop == null)
                return ancestors;

            while (shop?.ParentShopId != null)
            {
                shop = await GetByIdAsync(shop.ParentShopId.Value);
                if (shop != null)
                    ancestors.Add(shop);
            }

            ancestors.Reverse();
            return ancestors;
        }

        public async Task<List<Shop>> SearchAsync(string searchTerm, string? city = null, ShopStatus? status = null)
        {
            var query = _context.Shops
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(term) ||
                                        (s.Description != null && s.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(s => s.City != null && s.City.Contains(city));
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }
            else
            {
                query = query.Where(s => s.Status != ShopStatus.Archived);
            }

            return await query
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<Shop>> GetPaginatedAsync(int pageNumber, int pageSize, string? city = null, ShopStatus? status = null)
        {
            var query = _context.Shops
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(s => s.City != null && s.City.Contains(city));
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }
            else
            {
                query = query.Where(s => s.Status != ShopStatus.Archived);
            }

            return await query
                .OrderBy(s => s.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Shop> CreateAsync(Shop shop)
        {
            shop.Id = Guid.NewGuid();
            shop.CreatedAt = DateTime.UtcNow;
            shop.UpdatedAt = DateTime.UtcNow;
            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();
            return shop;
        }

        public async Task<Shop> UpdateAsync(Shop shop)
        {
            shop.UpdatedAt = DateTime.UtcNow;
            _context.Shops.Update(shop);
            await _context.SaveChangesAsync();
            return shop;
        }

        public async Task<bool> DeleteAsync(Guid shopId)
        {
            var shop = await GetByIdAsync(shopId);
            if (shop == null)
                return false;

            shop.SetStatus(ShopStatus.Archived);

            // Archive all child shops recursively
            var descendants = await GetDescendantsAsync(shopId);
            foreach (var descendant in descendants)
            {
                descendant.SetStatus(ShopStatus.Archived);
                _context.Shops.Update(descendant);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid shopId)
        {
            return await _context.Shops.AnyAsync(s => s.Id == shopId);
        }

        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Shops.AnyAsync(s => s.Name == name && s.Status != ShopStatus.Archived);
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Shops.CountAsync(s => s.Status != ShopStatus.Archived);
        }

        public async Task<int> GetCountByStatusAsync(ShopStatus status)
        {
            return await _context.Shops.CountAsync(s => s.Status == status);
        }

        public async Task<int> GetCountByOwnerAsync(Guid ownerId)
        {
            return await _context.Shops.CountAsync(s => s.OwnerUserId == ownerId && s.Status != ShopStatus.Archived);
        }

        private async Task LoadChildrenRecursively(Shop shop, int depth)
        {
            if (depth <= 0)
                return;

            var children = await _context.Shops
                .Where(s => s.ParentShopId == shop.Id)
                .ToListAsync();

            shop.ChildShops = children;

            foreach (var child in children)
            {
                await LoadChildrenRecursively(child, depth - 1);
            }
        }

        private async Task CollectDescendantsRecursively(Shop shop, List<Shop> descendants, int depth)
        {
            if (depth <= 0)
                return;

            var children = await _context.Shops
                .Where(s => s.ParentShopId == shop.Id)
                .ToListAsync();

            foreach (var child in children)
            {
                descendants.Add(child);
                await CollectDescendantsRecursively(child, descendants, depth - 1);
            }
        }
    }
}
