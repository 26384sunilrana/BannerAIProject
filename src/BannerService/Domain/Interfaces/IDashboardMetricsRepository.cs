namespace BannerService.Domain.Interfaces
{
    using Entities;
    using ValueObjects;

    /// <summary>
    /// Read-only aggregates for the admin and shop-owner dashboards, computed in the database.
    /// </summary>
    public interface IDashboardMetricsRepository
    {
        Task<ShopCounts> GetShopCountsAsync();
        Task<int> GetUserTotalCountAsync();

        /// <summary>All users when <paramref name="shopId"/> is null; otherwise that shop's users (role counts stay 0).</summary>
        Task<UserCounts> GetUserCountsAsync(Guid? shopId = null);

        /// <summary>All banners when <paramref name="shopId"/> is null; otherwise that shop's banners and media.</summary>
        Task<BannerCounts> GetBannerCountsAsync(Guid? shopId = null);

        Task<List<BannerSummary>> GetRecentBannerSummariesAsync(Guid shopId, int limit);

        /// <summary>Every plan, including inactive ones.</summary>
        Task<List<SubscriptionPlan>> GetAllPlansAsync();

        Task<PaymentCounts> GetPaymentCountsAsync();
        Task<int> GetSubscriptionCountAsync();
        Task<int> GetInvoiceCountAsync();
    }
}
