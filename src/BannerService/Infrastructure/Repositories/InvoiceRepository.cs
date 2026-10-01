namespace BannerService.Infrastructure.Repositories
{
    using Domain.Entities;
    using Domain.Interfaces;
    using Data;
    using Microsoft.EntityFrameworkCore;

    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context;

        public InvoiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Invoice?> GetByIdAsync(Guid invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.Subscription)
                .Include(i => i.Shop)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
        }

        public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
        {
            return await _context.Invoices
                .Include(i => i.Subscription)
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
        }

        public async Task<List<Invoice>> GetByShopIdAsync(Guid shopId)
        {
            return await _context.Invoices
                .Include(i => i.Subscription)
                .Where(i => i.ShopId == shopId)
                .OrderByDescending(i => i.IssuedDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId)
        {
            return await _context.Invoices
                .Include(i => i.Subscription)
                .Where(i => i.SubscriptionId == subscriptionId)
                .OrderByDescending(i => i.IssuedDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetByStatusAsync(InvoiceStatus status)
        {
            return await _context.Invoices
                .Include(i => i.Shop)
                .Include(i => i.Subscription)
                .Where(i => i.Status == status)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetOverdueAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.Invoices
                .Include(i => i.Shop)
                .Include(i => i.Subscription)
                .Where(i => i.DueDate < now && (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Overdue))
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetUnpaidAsync()
        {
            return await _context.Invoices
                .Include(i => i.Subscription)
                .Where(i => i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Overdue)
                .OrderBy(i => i.DueDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetPaginatedAsync(Guid shopId, int pageNumber, int pageSize)
        {
            return await _context.Invoices
                .Include(i => i.Subscription)
                .Where(i => i.ShopId == shopId)
                .OrderByDescending(i => i.IssuedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetCountByStatusAsync(InvoiceStatus status)
        {
            return await _context.Invoices.CountAsync(i => i.Status == status);
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Invoices.CountAsync();
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            invoice.Id = Guid.NewGuid();
            invoice.CreatedAt = DateTime.UtcNow;
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<Invoice> UpdateAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<bool> DeleteAsync(Guid invoiceId)
        {
            var invoice = await GetByIdAsync(invoiceId);
            if (invoice == null)
                return false;

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid invoiceId)
        {
            return await _context.Invoices.AnyAsync(i => i.Id == invoiceId);
        }

        public async Task<string> GenerateInvoiceNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var lastInvoice = await _context.Invoices
                .Where(i => i.InvoiceNumber.StartsWith($"INV-{year}-"))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                var parts = lastInvoice.InvoiceNumber.Split('-');
                if (int.TryParse(parts[2], out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            return $"INV-{year}-{nextNumber:D6}";
        }
    }
}
