namespace BannerService.Presentation.Controllers;

using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>Shop owner manages sales executive logins and chooses who approves banners.</summary>
[ApiController]
[Route("api/shops/{shopId}/team")]
[Authorize]
public class ShopTeamController : ControllerBase
{
    private readonly ShopTeamService _service;

    public ShopTeamController(ShopTeamService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    public Task<IActionResult> GetTeam(Guid shopId) =>
        Run(() => _service.GetTeamAsync(shopId, User.GetUserId()));

    /// <summary>Lets any member of the shop find out whether they own it and whether they approve banners.</summary>
    [HttpGet("my-role")]
    public Task<IActionResult> GetMyRole(Guid shopId) =>
        Run(() => _service.GetMyApprovalRoleAsync(shopId, User.GetUserId()));

    [HttpPost("executives")]
    public Task<IActionResult> AddSalesExecutive(Guid shopId, [FromBody] AddSalesExecutiveDto request) =>
        Run(async () => (object)await _service.AddSalesExecutiveAsync(shopId, User.GetUserId(), request), StatusCodes.Status201Created);

    [HttpDelete("executives/{userId}")]
    public Task<IActionResult> RemoveSalesExecutive(Guid shopId, string userId) =>
        Run(async () =>
        {
            await _service.RemoveSalesExecutiveAsync(shopId, User.GetUserId(), userId);
            return (object?)null;
        });

    /// <summary>After a takeover: the new owner confirms the approvers as they are.</summary>
    [HttpPost("confirm-approvers")]
    public Task<IActionResult> ConfirmApprovers(Guid shopId) => Run(() => _service.ConfirmApproversAsync(shopId, User.GetUserId()));

    [HttpPut("approvers")]
    public Task<IActionResult> SetApprovers(Guid shopId, [FromBody] SetApproversDto request) =>
        Run(() => _service.SetApproversAsync(shopId, User.GetUserId(), request));

    private async Task<IActionResult> Run<T>(Func<Task<T>> action, int successStatus = StatusCodes.Status200OK)
    {
        try
        {
            var result = await action();
            if (result == null)
                return NoContent();
            return successStatus == StatusCodes.Status201Created ? StatusCode(successStatus, result) : Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
