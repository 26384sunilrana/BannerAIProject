namespace BannerService.Domain.Entities
{
    public class SubscriptionPlan
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // Basic, Silver, Gold, Platinum
        public string Description { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public decimal AnnualPrice { get; set; }
        public int DisplayOrder { get; set; }
        public SubscriptionFeatures Features { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        public bool IsPopular => Name == "Silver" || Name == "Gold";

        public decimal GetPrice(BillingPeriod period) =>
            period == BillingPeriod.Monthly ? MonthlyPrice : AnnualPrice;

        public int GetBillingIntervalDays(BillingPeriod period) =>
            period == BillingPeriod.Monthly ? 30 : 365;
    }

    public enum BillingPeriod
    {
        Monthly = 1,
        Annual = 2
    }

    public class SubscriptionFeatures
    {
        public int MaxBanners { get; set; }
        public int MaxShops { get; set; }
        public int MaxUsers { get; set; }
        public int MaxStorageGB { get; set; }
        public bool ApiAccess { get; set; }
        public bool CustomDomain { get; set; }
        public bool AdvancedAnalytics { get; set; }
        public bool DedicatedSupport { get; set; }
        public decimal SlaPercentage { get; set; }
        public bool PriorityQueue { get; set; }

        // Predefined tier configurations
        public static SubscriptionFeatures CreateBasic() => new()
        {
            MaxBanners = 5,
            MaxShops = 1,
            MaxUsers = 1,
            MaxStorageGB = 1,
            ApiAccess = false,
            CustomDomain = false,
            AdvancedAnalytics = false,
            DedicatedSupport = false,
            SlaPercentage = 99.0m,
            PriorityQueue = false
        };

        public static SubscriptionFeatures CreateSilver() => new()
        {
            MaxBanners = 25,
            MaxShops = 5,
            MaxUsers = 3,
            MaxStorageGB = 10,
            ApiAccess = false,
            CustomDomain = false,
            AdvancedAnalytics = false,
            DedicatedSupport = false,
            SlaPercentage = 99.5m,
            PriorityQueue = false
        };

        public static SubscriptionFeatures CreateGold() => new()
        {
            MaxBanners = 1000,
            MaxShops = 25,
            MaxUsers = 10,
            MaxStorageGB = 100,
            ApiAccess = false,
            CustomDomain = true,
            AdvancedAnalytics = true,
            DedicatedSupport = false,
            SlaPercentage = 99.9m,
            PriorityQueue = true
        };

        public static SubscriptionFeatures CreatePlatinum() => new()
        {
            MaxBanners = 10000,
            MaxShops = 1000,
            MaxUsers = 1000,
            MaxStorageGB = 1000,
            ApiAccess = true,
            CustomDomain = true,
            AdvancedAnalytics = true,
            DedicatedSupport = true,
            SlaPercentage = 99.99m,
            PriorityQueue = true
        };
    }
}
