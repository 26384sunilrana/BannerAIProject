namespace BannerService.Domain.Interfaces;

using Entities;

public interface IShopAdRepository
{
    Task<ShopAd?> GetByIdAsync(Guid id);
    Task AddAsync(ShopAd ad, ShopAdEvent firstEvent);
    Task SaveAsync(ShopAd ad, ShopAdEvent? newEvent);

    /// <summary>Ads of a shop that hold a slot (waiting or approved) and touch the period.</summary>
    Task<List<ShopAd>> FindSlotHoldersAsync(Guid shopId, DateTime fromUtc, DateTime toUtc);

    /// <summary>Ads of one shop, or of every shop, that touch the period, newest start first.</summary>
    Task<List<ShopAd>> ListAsync(Guid? shopId, DateTime? fromUtc, DateTime? toUtc, bool holdingSlotOnly);

    /// <summary>Ads that were approved at some point (running, cancelled or overridden) and touch the period. One shop, or every shop.</summary>
    Task<List<ShopAd>> ListApprovedAsync(Guid? shopId, DateTime fromUtc, DateTime toUtc);

    Task<List<ShopAdEvent>> GetEventsAsync(Guid adId);
}
