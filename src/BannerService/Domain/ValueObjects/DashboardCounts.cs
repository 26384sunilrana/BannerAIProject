namespace BannerService.Domain.ValueObjects
{
    public class ShopCounts
    {
        public int Total { get; set; }
        public int TopLevel { get; set; }
        public int Active { get; set; }
        public int WithActiveSubscription { get; set; }
    }

    public class UserCounts
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int WithFailedLogin { get; set; }
        public int LockedOut { get; set; }
        public int AdminRole { get; set; }
        public int ShopOwnerRole { get; set; }
        public int SalesExecutiveRole { get; set; }
    }

    public class BannerCounts
    {
        public int Banners { get; set; }
        public int Published { get; set; }
        public int WithVersions { get; set; }
        public int Components { get; set; }
        public int Effects { get; set; }
        public int Carousels { get; set; }
        public long MediaBytes { get; set; }
    }

    public class PaymentCounts
    {
        public int Total { get; set; }
        public int Paid { get; set; }
        public int Overdue { get; set; }
        public int Refunded { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime? LastPaidDate { get; set; }
        public int SubscriptionsInGracePeriod { get; set; }
    }

    public class BannerSummary
    {
        public Guid BannerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int ComponentCount { get; set; }
        public int EffectCount { get; set; }
    }
}
