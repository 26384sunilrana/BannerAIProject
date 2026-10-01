namespace BannerService.Application.Tests.Controllers;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BannerService.Presentation.Controllers;
using Application.Services;
using Application.DTOs;
using Domain.Entities;

public class InvoicesControllerTests
{
    private readonly Mock<BillingService> _mockBillingService;
    private readonly Mock<RenewalService> _mockRenewalService;
    private readonly Mock<ILogger<InvoicesController>> _mockLogger;
    private readonly InvoicesController _controller;
    private readonly Guid _testInvoiceId;
    private readonly Guid _testShopId;
    private readonly Guid _testSubscriptionId;

    public InvoicesControllerTests()
    {
        _mockBillingService = new Mock<BillingService>(null, null, null);
        _mockRenewalService = new Mock<RenewalService>(null, null, null);
        _mockLogger = new Mock<ILogger<InvoicesController>>();
        _testInvoiceId = Guid.NewGuid();
        _testShopId = Guid.NewGuid();
        _testSubscriptionId = Guid.NewGuid();
        _controller = new InvoicesController(_mockBillingService.Object, _mockRenewalService.Object, _mockLogger.Object);
    }

    #region GetInvoice Tests

    [Fact]
    public async Task GetInvoice_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var invoice = new Invoice
        {
            Id = _testInvoiceId,
            ShopId = _testShopId,
            Amount = 100m,
            Status = InvoiceStatus.Draft
        };

        _mockBillingService.Setup(s => s.GetInvoiceByIdAsync(_testInvoiceId))
            .ReturnsAsync(invoice);

        // Act
        var result = await _controller.GetInvoice(_testInvoiceId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetInvoice_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var invalidInvoiceId = Guid.NewGuid();
        _mockBillingService.Setup(s => s.GetInvoiceByIdAsync(invalidInvoiceId))
            .ReturnsAsync((Invoice)null);

        // Act
        var result = await _controller.GetInvoice(invalidInvoiceId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    #endregion

    #region GetShopInvoices Tests

    [Fact]
    public async Task GetShopInvoices_WithValidShopId_ShouldReturnOk()
    {
        // Arrange
        var invoices = new List<Invoice>
        {
            new Invoice { Id = Guid.NewGuid(), ShopId = _testShopId, Amount = 100m },
            new Invoice { Id = Guid.NewGuid(), ShopId = _testShopId, Amount = 200m }
        };

        _mockBillingService.Setup(s => s.GetPaginatedInvoicesAsync(_testShopId, 1, 10))
            .ReturnsAsync(invoices);
        _mockBillingService.Setup(s => s.GetInvoiceCountAsync())
            .ReturnsAsync(2);

        // Act
        var result = await _controller.GetShopInvoices(_testShopId, 1, 10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetShopInvoices_WithInvalidPageNumber_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetShopInvoices(_testShopId, 0, 10);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region GetSubscriptionInvoices Tests

    [Fact]
    public async Task GetSubscriptionInvoices_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var invoices = new List<Invoice>
        {
            new Invoice { Id = Guid.NewGuid(), SubscriptionId = _testSubscriptionId },
            new Invoice { Id = Guid.NewGuid(), SubscriptionId = _testSubscriptionId }
        };

        _mockBillingService.Setup(s => s.GetSubscriptionInvoicesAsync(_testSubscriptionId))
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetSubscriptionInvoices(_testSubscriptionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetSubscriptionInvoices_WithNoInvoices_ShouldReturnEmptyList()
    {
        // Arrange
        _mockBillingService.Setup(s => s.GetSubscriptionInvoicesAsync(_testSubscriptionId))
            .ReturnsAsync(new List<Invoice>());

        // Act
        var result = await _controller.GetSubscriptionInvoices(_testSubscriptionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region GetInvoicesByStatus Tests

    [Fact]
    public async Task GetInvoicesByStatus_WithValidStatus_ShouldReturnOk()
    {
        // Arrange
        var invoices = new List<Invoice>
        {
            new Invoice { Id = Guid.NewGuid(), Status = InvoiceStatus.Paid }
        };

        _mockBillingService.Setup(s => s.GetInvoicesByStatusAsync(InvoiceStatus.Paid))
            .ReturnsAsync(invoices);

        // Act
        var result = await _controller.GetInvoicesByStatus("Paid");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetInvoicesByStatus_WithInvalidStatus_ShouldReturnBadRequest()
    {
        // Arrange
        // Act
        var result = await _controller.GetInvoicesByStatus("InvalidStatus");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    #endregion

    #region MarkAsPaid Tests

    [Fact]
    public async Task MarkAsPaid_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var request = new MarkInvoicePaidDto { PaymentReference = "REF123" };
        var invoice = new Invoice
        {
            Id = _testInvoiceId,
            Status = InvoiceStatus.Paid,
            PaymentReference = request.PaymentReference
        };

        _mockBillingService.Setup(s => s.MarkInvoiceAsPaidAsync(_testInvoiceId, request.PaymentReference))
            .ReturnsAsync((true, "Invoice marked as paid"));
        _mockBillingService.Setup(s => s.GetInvoiceByIdAsync(_testInvoiceId))
            .ReturnsAsync(invoice);

        // Act
        var result = await _controller.MarkAsPaid(_testInvoiceId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region CancelInvoice Tests

    [Fact]
    public async Task CancelInvoice_WithValidId_ShouldReturnOk()
    {
        // Arrange
        var request = new CancelInvoiceDto { Reason = "Test cancellation" };
        _mockBillingService.Setup(s => s.MarkInvoiceAsCancelledAsync(_testInvoiceId, request.Reason))
            .ReturnsAsync((true, "Invoice cancelled"));

        // Act
        var result = await _controller.CancelInvoice(_testInvoiceId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion

    #region CreateInvoice Tests

    [Fact]
    public async Task CreateInvoice_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new CreateInvoiceDto
        {
            SubscriptionId = _testSubscriptionId,
            ShopId = _testShopId,
            Amount = 100m,
            Description = "Monthly subscription"
        };

        _mockBillingService.Setup(s => s.CreateInvoiceAsync(
            request.SubscriptionId, request.ShopId, request.Amount, request.Description))
            .ReturnsAsync((true, "Invoice created"));

        // Act
        var result = await _controller.CreateInvoice(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    #endregion
}
