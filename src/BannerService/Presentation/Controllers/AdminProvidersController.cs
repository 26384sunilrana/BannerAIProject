namespace BannerService.Presentation.Controllers;

using System.Security.Claims;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security;

public class TestEmailDto
{
    /// <summary>Empty means the administrator's own address.</summary>
    public string? To { get; set; }
}

public class TestSmsDto
{
    public string To { get; set; } = string.Empty;
}

/// <summary>Administrator screen: are payments, SMS and e-mail set up, and a test button for each of the messages. Every call is audited.</summary>
[ApiController]
[Route("api/admin/providers")]
[Authorize(Roles = TenantAccess.AdminRole)]
public class AdminProvidersController : ControllerBase
{
    private readonly ProviderStatusService _status;

    public AdminProvidersController(ProviderStatusService status)
    {
        _status = status;
    }

    [HttpGet]
    public IActionResult GetStatus() => Ok(_status.GetStatus());

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailDto? request)
    {
        var to = string.IsNullOrWhiteSpace(request?.To) ? User.FindFirst(ClaimTypes.Email)?.Value : request!.To!.Trim();
        if (string.IsNullOrWhiteSpace(to) || !to.Contains('@'))
            return BadRequest(new { message = "Give an e-mail address", code = "invalid" });

        return Ok(await _status.SendTestEmailAsync(to));
    }

    [HttpPost("test-sms")]
    public async Task<IActionResult> TestSms([FromBody] TestSmsDto request)
    {
        if (string.IsNullOrWhiteSpace(request.To))
            return BadRequest(new { message = "Give a phone number", code = "invalid" });

        return Ok(await _status.SendTestSmsAsync(request.To));
    }
}
