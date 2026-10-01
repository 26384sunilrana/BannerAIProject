namespace BannerService.Domain.Tests.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using BannerService.Domain.Entities;
using BannerService.Infrastructure.Data;
using BannerService.Infrastructure.Repositories;

public class InvoiceRepositoryTests : IAsyncLifetime
{
    private DbContextOptions<ApplicationDbContext> _options;
    private ApplicationDbContext _context;
    private InvoiceRepository _repository;
    private readonly Guid _shopId = Guid.NewGuid();
    private readonly Guid _subscriptionId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(_options);
        _repository = new InvoiceRepository(_context);

        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedTestDataAsync()
    {
        var invoice1 = new Invoice
        {
            Id = Guid.NewGuid(),
            SubscriptionId = _subscriptionId,
            ShopId = _shopId,
            Amount = 99.99m,
            Status = InvoiceStatus.Issued,
            InvoiceNumber = $"INV-{DateTime.UtcNow.Year}-000001",
            IssuedDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Monthly subscription",
            CreatedAt = DateTime.UtcNow
        };

        var invoice2 = new Invoice
        {
            Id = Guid.NewGuid(),
            SubscriptionId = _subscriptionId,
            ShopId = _shopId,
            Amount = 199.99m,
            Status = InvoiceStatus.Paid,
            InvoiceNumber = $"INV-{DateTime.UtcNow.Year}-000002",
            IssuedDate = DateTime.UtcNow.AddDays(-30),
            DueDate = DateTime.UtcNow,
            PaidDate = DateTime.UtcNow,
            PaymentReference = "REF-12345",
            Description = "Monthly subscription",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        var overdueInvoice = new Invoice
        {
            Id = Guid.NewGuid(),
            SubscriptionId = _subscriptionId,
            ShopId = Guid.NewGuid(),
            Amount = 149.99m,
            Status = InvoiceStatus.Overdue,
            InvoiceNumber = $"INV-{DateTime.UtcNow.Year}-000003",
            IssuedDate = DateTime.UtcNow.AddDays(-60),
            DueDate = DateTime.UtcNow.AddDays(-30),
            Description = "Overdue invoice",
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };

        _context.Shops.AddRange(
            new Shop { Id = _shopId, Name = "Invoice Shop" },
            new Shop { Id = overdueInvoice.ShopId, Name = "Overdue Shop" });
        _context.Subscriptions.Add(new Subscription { Id = _subscriptionId, ShopId = _shopId });
        _context.Invoices.AddRange(invoice1, invoice2, overdueInvoice);
        await _context.SaveChangesAsync();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnInvoice()
    {
        // Arrange
        var invoice = _context.Invoices.First();

        // Act
        var result = await _repository.GetByIdAsync(invoice.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoice.Id, result.Id);
        Assert.Equal(invoice.InvoiceNumber, result.InvoiceNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByInvoiceNumberAsync Tests

    [Fact]
    public async Task GetByInvoiceNumberAsync_WithValidNumber_ShouldReturnInvoice()
    {
        // Arrange
        var invoice = _context.Invoices.First();

        // Act
        var result = await _repository.GetByInvoiceNumberAsync(invoice.InvoiceNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(invoice.InvoiceNumber, result.InvoiceNumber);
    }

    [Fact]
    public async Task GetByInvoiceNumberAsync_WithInvalidNumber_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByInvoiceNumberAsync("INV-2099-999999");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByShopIdAsync Tests

    [Fact]
    public async Task GetByShopIdAsync_WithValidShopId_ShouldReturnInvoices()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(_shopId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, i => Assert.Equal(_shopId, i.ShopId));
    }

    [Fact]
    public async Task GetByShopIdAsync_ShouldBeOrderedByIssuedDateDescending()
    {
        // Act
        var results = await _repository.GetByShopIdAsync(_shopId);

        // Assert
        if (results.Count > 1)
        {
            for (int i = 0; i < results.Count - 1; i++)
            {
                Assert.True(results[i].IssuedDate >= results[i + 1].IssuedDate);
            }
        }
    }

    #endregion

    #region GetBySubscriptionIdAsync Tests

    [Fact]
    public async Task GetBySubscriptionIdAsync_WithValidSubscriptionId_ShouldReturnInvoices()
    {
        // Act
        var results = await _repository.GetBySubscriptionIdAsync(_subscriptionId);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, i => Assert.Equal(_subscriptionId, i.SubscriptionId));
    }

    #endregion

    #region GetByStatusAsync Tests

    [Fact]
    public async Task GetByStatusAsync_WithStatus_ShouldReturnInvoicesWithStatus()
    {
        // Act
        var results = await _repository.GetByStatusAsync(InvoiceStatus.Paid);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, i => Assert.Equal(InvoiceStatus.Paid, i.Status));
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldBeOrderedByDueDate()
    {
        // Act
        var results = await _repository.GetByStatusAsync(InvoiceStatus.Issued);

        // Assert
        if (results.Count > 1)
        {
            var dueDates = results.Select(i => i.DueDate).ToList();
            var sortedDates = dueDates.OrderBy(d => d).ToList();
            Assert.Equal(sortedDates, dueDates);
        }
    }

    #endregion

    #region GetOverdueAsync Tests

    [Fact]
    public async Task GetOverdueAsync_ShouldReturnOverdueInvoices()
    {
        // Act
        var results = await _repository.GetOverdueAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, i => Assert.True(i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Overdue));
        Assert.All(results, i => Assert.True(i.DueDate < DateTime.UtcNow));
    }

    #endregion

    #region GetUnpaidAsync Tests

    [Fact]
    public async Task GetUnpaidAsync_ShouldReturnUnpaidInvoices()
    {
        // Act
        var results = await _repository.GetUnpaidAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, i =>
            Assert.True(i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.Overdue)
        );
    }

    #endregion

    #region GetPaginatedAsync Tests

    [Fact]
    public async Task GetPaginatedAsync_WithValidPagination_ShouldReturnPagedResults()
    {
        // Act
        var page1 = await _repository.GetPaginatedAsync(_shopId, pageNumber: 1, pageSize: 1);

        // Assert
        Assert.Single(page1);
    }

    [Fact]
    public async Task GetPaginatedAsync_WithPage2_ShouldReturnSecondPage()
    {
        // Act
        var page1 = await _repository.GetPaginatedAsync(_shopId, pageNumber: 1, pageSize: 1);
        var page2 = await _repository.GetPaginatedAsync(_shopId, pageNumber: 2, pageSize: 1);

        // Assert
        Assert.NotEqual(page1.First().Id, page2.First().Id);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidInvoice_ShouldAddToDatabase()
    {
        // Arrange
        var newInvoice = new Invoice
        {
            SubscriptionId = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            Amount = 299.99m,
            Status = InvoiceStatus.Issued,
            InvoiceNumber = $"INV-{DateTime.UtcNow.Year}-000010",
            IssuedDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "New invoice"
        };

        // Act
        var result = await _repository.CreateAsync(newInvoice);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await _context.Invoices.FindAsync(result.Id);
        Assert.NotNull(saved);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidInvoice_ShouldUpdateDatabase()
    {
        // Arrange
        var invoice = _context.Invoices.First(i => i.Status == InvoiceStatus.Issued);
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidDate = DateTime.UtcNow;

        // Act
        await _repository.UpdateAsync(invoice);

        // Assert
        var updated = await _context.Invoices.FindAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.Paid, updated.Status);
        Assert.NotNull(updated.PaidDate);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemoveFromDatabase()
    {
        // Arrange
        var invoice = _context.Invoices.First();

        // Act
        var result = await _repository.DeleteAsync(invoice.Id);

        // Assert
        Assert.True(result);
        var deleted = await _context.Invoices.FindAsync(invoice.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var invoice = _context.Invoices.First();

        // Act
        var result = await _repository.ExistsAsync(invoice.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_WithNonexistentId_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        Assert.True(count > 0);
    }

    [Fact]
    public async Task GetCountByStatusAsync_ShouldReturnCorrectCount()
    {
        // Act
        var count = await _repository.GetCountByStatusAsync(InvoiceStatus.Issued);

        // Assert
        Assert.True(count > 0);
    }

    #endregion

    #region GenerateInvoiceNumberAsync Tests

    [Fact]
    public async Task GenerateInvoiceNumberAsync_ShouldGenerateUniqueNumber()
    {
        // Act
        var number1 = await _repository.GenerateInvoiceNumberAsync();
        _context.Invoices.Add(new Invoice
        {
            Id = Guid.NewGuid(),
            SubscriptionId = _subscriptionId,
            ShopId = _shopId,
            InvoiceNumber = number1,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        var number2 = await _repository.GenerateInvoiceNumberAsync();

        // Assert
        Assert.NotEqual(number1, number2);
        Assert.StartsWith($"INV-{DateTime.UtcNow.Year}-", number1);
    }

    #endregion
}
