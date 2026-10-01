namespace BannerService.Presentation.Controllers
{
    using Application.DTOs;
    using Application.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;

    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            IAuthenticationService authenticationService,
            ILogger<AuthenticationController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.UserAgent = Request.Headers["User-Agent"];

            var (success, message, tokens) = await _authenticationService.RegisterAsync(request);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message, tokens });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.UserAgent = Request.Headers["User-Agent"];

            var (success, message, tokens) = await _authenticationService.LoginAsync(request);

            if (!success)
                return Unauthorized(new { message });

            return Ok(new { message, tokens });
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

            var (success, message, tokens) = await _authenticationService.RefreshTokenAsync(
                request.RefreshToken,
                userId
            );

            if (!success)
                return Unauthorized(new { message });

            return Ok(new { message, tokens });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { message = "User ID not found in token" });

            var success = await _authenticationService.RevokeTokenAsync(userId, request.RefreshToken);

            if (!success)
                return BadRequest(new { message = "Failed to revoke token" });

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var shopId = User.FindFirst("ShopId")?.Value;
            var roles = User.FindAll(ClaimTypes.Role);

            return Ok(new
            {
                userId,
                email,
                name,
                shopId,
                roles = roles.Select(r => r.Value).ToList()
            });
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var (success, message) = await _authenticationService.VerifyEmailAsync(userId, request.Token);

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] RequestPasswordResetDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message) = await _authenticationService.RequestPasswordResetAsync(request.Email);

            // Return success regardless of whether email exists (security best practice)
            return Ok(new { message });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User not authenticated" });

            var (success, message) = await _authenticationService.ResetPasswordAsync(
                userId,
                request.Token,
                request.NewPassword
            );

            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health()
        {
            return Ok(new { message = "Authentication service is healthy" });
        }
    }
}
