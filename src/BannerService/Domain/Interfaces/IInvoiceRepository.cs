namespace BannerService.Domain.Interfaces
{
    using Entities;

    public interface IInvoiceRepository
    {
        Task<Invoice?> GetByIdAsync(Guid invoiceId);
        Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
        Task<List<Invoice>> GetByShopIdAsync(Guid shopId);
        Task<List<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId);
        Task<List<Invoice>> GetByStatusAsync(InvoiceStatus status);
        Task<List<Invoice>> GetOverdueAsync();
        Task<List<Invoice>> GetUnpaidAsync();
        Task<List<Invoice>> GetPaginatedAsync(Guid shopId, int pageNumber, int pageSize);
        Task<int> GetCountByStatusAsync(InvoiceStatus status);
        Task<int> GetCountAsync();
        Task<Invoice> CreateAsync(Invoice invoice);
        Task<Invoice> UpdateAsync(Invoice invoice);
        Task<bool> DeleteAsync(Guid invoiceId);
        Task<bool> ExistsAsync(Guid invoiceId);
        Task<string> GenerateInvoiceNumberAsync();
    }
}
