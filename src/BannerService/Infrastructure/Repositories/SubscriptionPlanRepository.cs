namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class SubscriptionPlanRepository : ISubscriptionPlanRepository
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionPlanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SubscriptionPlan?> GetByIdAsync(Guid planId)
        {
            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == planId);
        }

        public async Task<SubscriptionPlan?> GetByNameAsync(string name)
        {
            return await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Name == name && p.IsActive);
        }

        public async Task<List<SubscriptionPlan>> GetAllAsync()
        {
            return await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<SubscriptionPlan>> GetActiveAsync()
        {
            return await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.SubscriptionPlans.CountAsync(p => p.IsActive);
        }

        public async Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan)
        {
            plan.Id = Guid.NewGuid();
            plan.CreatedAt = DateTime.UtcNow;
            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<SubscriptionPlan> UpdateAsync(SubscriptionPlan plan)
        {
            _context.SubscriptionPlans.Update(plan);
            await _context.SaveChangesAsync();
            return plan;
        }

        public async Task<bool> DeleteAsync(Guid planId)
        {
            var plan = await GetByIdAsync(planId);
            if (plan == null)
                return false;

            plan.IsActive = false;
            await UpdateAsync(plan);
            return true;
        }

        public async Task<bool> ExistsAsync(Guid planId)
        {
            return await _context.SubscriptionPlans.AnyAsync(p => p.Id == planId);
        }
    }
}
