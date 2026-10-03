namespace BannerService.Domain.Interfaces;

using Entities;

public interface IShopTakeoverRepository
{
    Task<ShopTakeover?> GetByIdAsync(Guid id);
    Task AddAsync(ShopTakeover takeover);
    Task SaveAsync();

    /// <summary>Open requests (not yet answered) for a shop that is being taken over.</summary>
    Task<List<ShopTakeover>> OpenForExistingShopAsync(Guid existingShopId);

    /// <summary>Requests made by a shop, or about a shop: every state, newest first.</summary>
    Task<List<ShopTakeover>> ListForShopAsync(Guid shopId);

    /// <summary>Every request, newest first, for administrators. At most 500.</summary>
    Task<List<ShopTakeover>> ListAllAsync();
}
