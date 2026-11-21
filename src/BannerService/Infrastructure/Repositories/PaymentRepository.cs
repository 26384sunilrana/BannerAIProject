namespace BannerService.Infrastructure.Repositories;

using Domain.Entities;
using Domain.Interfaces;
using Data;
using Microsoft.EntityFrameworkCore;

public class PaymentRepository : IPaymentRepository
{
    public static readonly TimeSpan UnpaidAttemptLife = TimeSpan.FromDays(7);
    public static readonly TimeSpan ReceiptLife = TimeSpan.FromDays(180);

    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentAttempt> AddAttemptAsync(PaymentAttempt attempt)
    {
        _context.PaymentAttempts.Add(attempt);
        await _context.SaveChangesAsync();
        return attempt;
    }

    public Task<PaymentAttempt?> GetAttemptAsync(Guid attemptId) =>
        _context.PaymentAttempts.Include(a => a.Refunds).FirstOrDefaultAsync(a => a.Id == attemptId);

    public Task<PaymentAttempt?> GetAttemptByOrderIdAsync(string provider, string providerOrderId) =>
        _context.PaymentAttempts.Include(a => a.Refunds)
            .FirstOrDefaultAsync(a => a.Provider == provider && a.ProviderOrderId == providerOrderId);

    public Task<List<PaymentAttempt>> ListForShopAsync(Guid shopId) =>
        _context.PaymentAttempts.Include(a => a.Refunds)
            .Where(a => a.ShopId == shopId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(200)
            .ToListAsync();

    public async Task UpdateAttemptAsync(PaymentAttempt attempt)
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new PaymentConcurrencyException(ex);
        }
    }

    public async Task AddRefundAsync(Refund refund)
    {
        // the refund is already part of its attempt's collection; saving stores both
        if (_context.Entry(refund).State == EntityState.Detached)
            _context.Refunds.Add(refund);
        await _context.SaveChangesAsync();
    }

    public async Task<(WebhookReceipt receipt, bool alreadyProcessed)> RecordReceiptAsync(string provider, string providerEventId, string eventType)
    {
        var existing = await _context.WebhookReceipts.FirstOrDefaultAsync(r => r.Provider == provider && r.ProviderEventId == providerEventId);
        if (existing != null)
            return (existing, existing.ProcessedAt != null);

        var receipt = new WebhookReceipt { Provider = provider, ProviderEventId = providerEventId, EventType = eventType };
        _context.WebhookReceipts.Add(receipt);
        try
        {
            await _context.SaveChangesAsync();
            return (receipt, false);
        }
        catch (DbUpdateException)
        {
            // another request recorded the same event between our check and our insert
            _context.Entry(receipt).State = EntityState.Detached;
            var winner = await _context.WebhookReceipts.FirstOrDefaultAsync(r => r.Provider == provider && r.ProviderEventId == providerEventId);
            if (winner == null)
                throw;
            return (winner, winner.ProcessedAt != null);
        }
    }

    public async Task UpdateReceiptAsync(WebhookReceipt receipt)
    {
        _context.WebhookReceipts.Update(receipt);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CleanUpAsync(DateTime nowUtc)
    {
        var attemptCutoff = nowUtc - UnpaidAttemptLife;
        var receiptCutoff = nowUtc - ReceiptLife;

        var staleAttempts = await _context.PaymentAttempts
            .Where(a => a.Status == PaymentAttemptStatus.Created && a.CreatedAt < attemptCutoff)
            .ToListAsync();
        _context.PaymentAttempts.RemoveRange(staleAttempts);

        var oldReceipts = await _context.WebhookReceipts.Where(r => r.ReceivedAt < receiptCutoff).ToListAsync();
        _context.WebhookReceipts.RemoveRange(oldReceipts);

        await _context.SaveChangesAsync();
        return staleAttempts.Count + oldReceipts.Count;
    }
}
