namespace BannerService.Application.Services
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Domain.ValueObjects;

    public class AdvertisementService : IAdvertisementService
    {
        private readonly IAdvertisementRepository _repository;

        public AdvertisementService(IAdvertisementRepository repository)
        {
            _repository = repository;
        }

        public async Task<Advertisement> CreateAdAsync(Guid shopId, string title, string description, AdType type, Guid userId)
        {
            var ad = new Advertisement
            {
                ShopId = shopId,
                CreatedByUserId = userId,
                Title = title,
                Description = description,
                Type = type,
                Status = AdStatus.Draft,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            return await _repository.CreateAsync(ad);
        }

        public async Task<Advertisement> GetAdAsync(Guid adId)
        {
            var ad = await _repository.GetByIdAsync(adId);
            if (ad == null)
                throw new KeyNotFoundException($"Advertisement {adId} not found");
            return ad;
        }

        public async Task<List<Advertisement>> GetShopAdsAsync(Guid shopId)
        {
            return await _repository.GetByShopIdAsync(shopId);
        }

        public async Task<List<Advertisement>> GetActiveAdsAsync()
        {
            return await _repository.GetActiveAdsAsync();
        }

        public async Task<Advertisement> UpdateAdAsync(Guid adId, string title, string description, Guid userId)
        {
            var ad = await GetAdAsync(adId);

            if (!ad.CanBeEdited())
                throw new InvalidOperationException("Can only edit advertisements in draft status");

            ad.Title = title;
            ad.Description = description;
            ad.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(ad);
        }

        public async Task<Advertisement> SetAdTargetAsync(Guid adId, AdTarget target, Guid userId)
        {
            var ad = await GetAdAsync(adId);

            if (!target.IsValid())
                throw new InvalidOperationException("Advertisement target is invalid");

            ad.Target = target;
            ad.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(ad);
        }

        public async Task<Advertisement> SetBudgetAsync(Guid adId, decimal budgetLimit, decimal dailyBudget, Guid userId)
        {
            var ad = await GetAdAsync(adId);

            if (budgetLimit < 0 || dailyBudget < 0)
                throw new ArgumentException("Budget values cannot be negative");

            if (dailyBudget > budgetLimit && budgetLimit > 0)
                throw new ArgumentException("Daily budget cannot exceed total budget");

            ad.BudgetLimit = budgetLimit;
            ad.DailyBudgetLimit = dailyBudget;
            ad.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(ad);
        }

        public async Task<Advertisement> SetDatesAsync(Guid adId, DateTime startDate, DateTime endDate, Guid userId)
        {
            var ad = await GetAdAsync(adId);

            if (startDate >= endDate)
                throw new ArgumentException("Start date must be before end date");

            if (startDate < DateTime.UtcNow)
                throw new ArgumentException("Start date cannot be in the past");

            ad.StartDate = startDate;
            ad.EndDate = endDate;
            ad.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateAsync(ad);
        }

        public async Task<Advertisement> ActivateAdAsync(Guid adId, Guid userId)
        {
            var ad = await GetAdAsync(adId);
            ad.Activate(userId);
            return await _repository.UpdateAsync(ad);
        }

        public async Task<Advertisement> PauseAdAsync(Guid adId, Guid userId)
        {
            var ad = await GetAdAsync(adId);
            ad.Pause(userId);
            return await _repository.UpdateAsync(ad);
        }

        public async Task<Advertisement> CompleteAdAsync(Guid adId, Guid userId)
        {
            var ad = await GetAdAsync(adId);
            ad.Complete(userId);
            return await _repository.UpdateAsync(ad);
        }

        public async Task<Advertisement> CancelAdAsync(Guid adId, Guid userId)
        {
            var ad = await GetAdAsync(adId);
            ad.Cancel(userId);
            return await _repository.UpdateAsync(ad);
        }

        public async Task<Advertisement> ArchiveAdAsync(Guid adId, Guid userId)
        {
            var ad = await GetAdAsync(adId);
            ad.Archive(userId);
            return await _repository.UpdateAsync(ad);
        }

        public async Task<bool> DeleteAdAsync(Guid adId, Guid userId)
        {
            var ad = await _repository.GetByIdAsync(adId);
            if (ad == null || ad.Status != AdStatus.Draft)
                return false;

            return await _repository.DeleteAsync(adId);
        }

        public async Task<List<Advertisement>> GetExpiringAdsAsync(int daysThreshold = 7)
        {
            return await _repository.GetExpiringAdsAsync(daysThreshold);
        }

        public async Task UpdateMetricsAsync(Guid adId, long impressions, long clicks, long conversions, decimal spend)
        {
            var ad = await GetAdAsync(adId);

            ad.Metrics.Impressions = impressions;
            ad.Metrics.Clicks = clicks;
            ad.Metrics.Conversions = conversions;
            ad.Metrics.TotalSpent = spend;
            ad.Metrics.LastUpdatedAt = DateTime.UtcNow;

            if (ad.IsBudgetExceeded() && ad.Status == AdStatus.Active)
            {
                ad.Complete(Guid.Empty);
            }

            await _repository.UpdateAsync(ad);
        }

        public async Task<decimal> CalculateROIAsync(Guid adId)
        {
            var ad = await GetAdAsync(adId);
            return ad.Metrics.GetROI();
        }

        public async Task<List<Advertisement>> SearchAdsAsync(Guid shopId, string searchTerm)
        {
            var ads = await _repository.GetByShopIdAsync(shopId);
            var lowerSearch = searchTerm.ToLower();

            return ads
                .Where(a => a.Title.ToLower().Contains(lowerSearch) ||
                           a.Description.ToLower().Contains(lowerSearch))
                .ToList();
        }
    }
}
