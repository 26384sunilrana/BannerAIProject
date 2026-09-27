namespace BannerService.Domain.Tests.Entities;

using System;
using Xunit;
using BannerService.Domain.Entities;

public class InvoiceEntityTests
{
    #region Constructor Tests

    [Fact]
    public void CreateInvoice_WithValidData_ShouldInitializeProperties()
    {
        // Act
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ShopId = Guid.NewGuid(),
            SubscriptionId = Guid.NewGuid(),
            Amount = 99.99m,
            Status = InvoiceStatus.Issued,
            InvoiceNumber = "INV-2024-001",
            IssuedDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        // Assert
        Assert.NotEqual(Guid.Empty, invoice.Id);
        Assert.Equal(99.99m, invoice.Amount);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    #endregion

    #region IsPaid Property Tests

    [Fact]
    public void IsPaid_WhenStatusIsPaid_ShouldReturnTrue()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Paid };

        // Act
        var result = invoice.IsPaid;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPaid_WhenStatusIsIssued_ShouldReturnFalse()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Issued };

        // Act
        var result = invoice.IsPaid;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsDue Property Tests

    [Fact]
    public void IsDue_WhenPastDueAndUnpaid_ShouldReturnTrue()
    {
        // Arrange
        var invoice = new Invoice
        {
            DueDate = DateTime.UtcNow.AddDays(-1),
            Status = InvoiceStatus.Issued
        };

        // Act
        var result = invoice.IsDue;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDue_WhenPaidButPastDue_ShouldReturnFalse()
    {
        // Arrange
        var invoice = new Invoice
        {
            DueDate = DateTime.UtcNow.AddDays(-1),
            Status = InvoiceStatus.Paid
        };

        // Act
        var result = invoice.IsDue;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsOverdue Property Tests

    [Fact]
    public void IsOverdue_WhenPastDueAndNotPaid_ShouldReturnTrue()
    {
        // Arrange
        var invoice = new Invoice
        {
            DueDate = DateTime.UtcNow.AddDays(-5),
            Status = InvoiceStatus.Issued
        };

        // Act
        var result = invoice.IsOverdue;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsOverdue_WhenCancelled_ShouldReturnFalse()
    {
        // Arrange
        var invoice = new Invoice
        {
            DueDate = DateTime.UtcNow.AddDays(-1),
            Status = InvoiceStatus.Cancelled
        };

        // Act
        var result = invoice.IsOverdue;

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DaysOverdue Property Tests

    [Fact]
    public void DaysOverdue_WhenOverdueByFiveDays_ShouldReturnFive()
    {
        // Arrange
        var invoice = new Invoice
        {
            DueDate = DateTime.UtcNow.AddDays(-5),
            Status = InvoiceStatus.Issued
        };

        // Act
        var days = invoice.DaysOverdue;

        // Assert
        Assert.True(days >= 4 && days <= 6);
    }

    [Fact]
    public void DaysOverdue_WhenNotOverdue_ShouldReturnZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            DueDate = DateTime.UtcNow.AddDays(5),
            Status = InvoiceStatus.Issued
        };

        // Act
        var days = invoice.DaysOverdue;

        // Assert
        Assert.Equal(0, days);
    }

    #endregion

    #region AmountDue Property Tests

    [Fact]
    public void AmountDue_WhenUnpaid_ShouldEqualInvoiceAmount()
    {
        // Arrange
        var invoice = new Invoice
        {
            Amount = 99.99m,
            Status = InvoiceStatus.Issued
        };

        // Act
        var amountDue = invoice.AmountDue;

        // Assert
        Assert.Equal(99.99m, amountDue);
    }

    [Fact]
    public void AmountDue_WhenPaid_ShouldBeZero()
    {
        // Arrange
        var invoice = new Invoice
        {
            Amount = 99.99m,
            Status = InvoiceStatus.Paid
        };

        // Act
        var amountDue = invoice.AmountDue;

        // Assert
        Assert.Equal(0, amountDue);
    }

    #endregion

    #region MarkAsPaid Tests

    [Fact]
    public void MarkAsPaid_ShouldSetStatusAndPaidDate()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Issued };
        var beforeTime = DateTime.UtcNow;

        // Act
        invoice.MarkAsPaid();

        // Assert
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidDate);
        Assert.True(invoice.PaidDate >= beforeTime);
    }

    [Fact]
    public void MarkAsPaid_WithPaymentReference_ShouldSetReference()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Issued };
        var reference = "PAY-12345";

        // Act
        invoice.MarkAsPaid(reference);

        // Assert
        Assert.Equal(reference, invoice.PaymentReference);
    }

    #endregion

    #region MarkAsOverdue Tests

    [Fact]
    public void MarkAsOverdue_WhenUnpaid_ShouldSetStatus()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Issued };

        // Act
        invoice.MarkAsOverdue();

        // Assert
        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    [Fact]
    public void MarkAsOverdue_WhenAlreadyPaid_ShouldNotChange()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Paid };

        // Act
        invoice.MarkAsOverdue();

        // Assert
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public void Cancel_ShouldSetStatusToCancelled()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Issued };

        // Act
        invoice.Cancel();

        // Assert
        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);
    }

    #endregion

    #region Refund Tests

    [Fact]
    public void Refund_ShouldSetStatusToRefunded()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Paid };

        // Act
        invoice.Refund();

        // Assert
        Assert.Equal(InvoiceStatus.Refunded, invoice.Status);
    }

    #endregion

    #region GetStatusDisplay Tests

    [Fact]
    public void GetStatusDisplay_WithDraftStatus_ShouldReturnDisplay()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Draft };

        // Act
        var display = invoice.GetStatusDisplay();

        // Assert
        Assert.Equal("Draft", display);
    }

    [Fact]
    public void GetStatusDisplay_WithPaidStatus_ShouldReturnDisplay()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Paid };

        // Act
        var display = invoice.GetStatusDisplay();

        // Assert
        Assert.Equal("Paid", display);
    }

    [Fact]
    public void GetStatusDisplay_WithOverdueStatus_ShouldReturnDisplay()
    {
        // Arrange
        var invoice = new Invoice { Status = InvoiceStatus.Overdue };

        // Act
        var display = invoice.GetStatusDisplay();

        // Assert
        Assert.Equal("Overdue", display);
    }

    #endregion
}
