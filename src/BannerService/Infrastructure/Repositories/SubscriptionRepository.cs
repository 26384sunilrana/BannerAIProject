namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Subscription?> GetByIdAsync(Guid subscriptionId)
        {
            return await _context.Subscriptions
                .Include(s => s.Shop)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);
        }

        public async Task<Subscription?> GetByShopIdAsync(Guid shopId)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.ShopId == shopId && s.Status != SubscriptionStatus.Cancelled)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Subscription>> GetActiveByShopIdAsync(Guid shopId)
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.ShopId == shopId && s.Status == SubscriptionStatus.Active)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetByStatusAsync(SubscriptionStatus status)
        {
            return await _context.Subscriptions
                .Include(s => s.Shop)
                .Include(s => s.Plan)
                .Where(s => s.Status == status)
                .OrderBy(s => s.RenewalDate)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetExpiringTodayAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await _context.Subscriptions
                .Include(s => s.Shop)
                .Include(s => s.Plan)
                .Where(s => s.RenewalDate >= today && s.RenewalDate < tomorrow &&
                            s.Status == SubscriptionStatus.Active)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetRenewingSoonAsync(int daysThreshold = 7)
        {
            var today = DateTime.UtcNow;
            var threshold = today.AddDays(daysThreshold);

            return await _context.Subscriptions
                .Include(s => s.Shop)
                .Include(s => s.Plan)
                .Where(s => s.RenewalDate >= today && s.RenewalDate <= threshold &&
                            s.Status == SubscriptionStatus.Active)
                .OrderBy(s => s.RenewalDate)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetWithPaymentFailuresAsync()
        {
            return await _context.Subscriptions
                .Include(s => s.Shop)
                .Include(s => s.Plan)
                .Where(s => s.PaymentFailureCount > 0 &&
                           (s.Status == SubscriptionStatus.PaymentFailed ||
                            s.Status == SubscriptionStatus.Active))
                .OrderBy(s => s.LastPaymentAttempt)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetInGracePeriodAsync()
        {
            return await _context.Subscriptions
                .Include(s => s.Shop)
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.GracePeriod)
                .OrderBy(s => s.RenewalDate)
                .ToListAsync();
        }

        public async Task<List<Subscription>> GetAllAsync()
        {
            return await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status != SubscriptionStatus.Cancelled)
                .ToListAsync();
        }

        public async Task<int> GetCountByStatusAsync(SubscriptionStatus status)
        {
            return await _context.Subscriptions.CountAsync(s => s.Status == status);
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Subscriptions.CountAsync(s => s.Status != SubscriptionStatus.Cancelled);
        }

        public async Task<Subscription> CreateAsync(Subscription subscription)
        {
            subscription.Id = Guid.NewGuid();
            subscription.CreatedAt = DateTime.UtcNow;
            subscription.UpdatedAt = DateTime.UtcNow;
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<Subscription> UpdateAsync(Subscription subscription)
        {
            subscription.UpdatedAt = DateTime.UtcNow;
            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<bool> DeleteAsync(Guid subscriptionId)
        {
            var subscription = await GetByIdAsync(subscriptionId);
            if (subscription == null)
                return false;

            subscription.Cancel();
            await UpdateAsync(subscription);
            return true;
        }

        public async Task<bool> ExistsAsync(Guid subscriptionId)
        {
            return await _context.Subscriptions.AnyAsync(s => s.Id == subscriptionId);
        }
    }
}
