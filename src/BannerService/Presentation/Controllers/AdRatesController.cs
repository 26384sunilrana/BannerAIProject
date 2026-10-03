namespace BannerService.Presentation.Controllers;

using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>The administrator's price list for ads.</summary>
[ApiController]
[Route("api/ad-rates")]
[Authorize(Roles = "Admin")]
public class AdRatesController : ControllerBase
{
    private readonly AdRateService _rates;

    public AdRatesController(AdRateService rates)
    {
        _rates = rates;
    }

    [HttpGet]
    public Task<IActionResult> List() => ServiceErrors.Run(this, () => _rates.ListAsync());

    [HttpPost]
    public Task<IActionResult> Create([FromBody] AdRateInput input) => ServiceErrors.Run(this, () => _rates.CreateAsync(input));

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] AdRateInput input) => ServiceErrors.Run(this, () => _rates.UpdateAsync(id, input));

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Deactivate(Guid id) => ServiceErrors.Run(this, () => _rates.DeactivateAsync(id));
}

/// <summary>The monthly statement of ads: hours run and what each shop is paid for the administrator's ads.</summary>
[ApiController]
[Route("api/ad-statements")]
[Authorize(Roles = "Admin,ShopOwner")]
public class AdStatementsController : ControllerBase
{
    private readonly AdStatementService _statements;

    public AdStatementsController(AdStatementService statements)
    {
        _statements = statements;
    }

    [HttpGet]
    public Task<IActionResult> Get([FromQuery] string month, [FromQuery] Guid? shopId)
    {
        var admin = User.IsInRole("Admin");
        var actor = new AdActor(User.GetUserId(), User.GetUserName(), admin ? ShopAdSource.Admin : ShopAdSource.ShopOwner, admin ? null : User.GetShopId());
        return ServiceErrors.Run(this, () => _statements.GetAsync(actor, month ?? string.Empty, shopId));
    }
}

/// <summary>Who booked how many ads, for administrators.</summary>
[ApiController]
[Route("api/ad-reports")]
[Authorize(Roles = "Admin")]
public class AdReportsController : ControllerBase
{
    private readonly AdReportService _reports;

    public AdReportsController(AdReportService reports)
    {
        _reports = reports;
    }

    [HttpGet("users")]
    public Task<IActionResult> Users([FromQuery] string month) => ServiceErrors.Run(this, () => _reports.UsersAsync(month ?? string.Empty));
}
