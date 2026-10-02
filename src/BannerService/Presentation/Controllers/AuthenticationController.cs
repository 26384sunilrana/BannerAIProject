namespace BannerService.Presentation.Controllers
{
    using Application.DTOs;
    using Application.Services;
    using Infrastructure.Security;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Claims;

    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        /// <summary>
        /// Sent by the web app on calls that rely on the refresh cookie. A page on another site cannot add a custom header
        /// without a CORS pre-flight, which the API only grants to its own web app - this is the protection against forged requests.
        /// </summary>
        public const string CsrfHeader = "X-Requested-With";

        private readonly IAuthenticationService _authenticationService;
        private readonly IRefreshCookie _cookie;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            IAuthenticationService authenticationService,
            IRefreshCookie cookie,
            ILogger<AuthenticationController> logger)
        {
            _authenticationService = authenticationService;
            _cookie = cookie;
            _logger = logger;
        }

        /// <summary>Answers with the tokens. With cookies on, the refresh token goes into the cookie and is left out of the body.</summary>
        private IActionResult Issue(string message, AuthTokenDto tokens)
        {
            if (!_cookie.Enabled)
                return Ok(new { message, tokens });

            _cookie.Write(Response, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
            return Ok(new
            {
                message,
                tokens = new AuthTokenDto
                {
                    AccessToken = tokens.AccessToken,
                    RefreshToken = string.Empty,
                    ExpiresIn = tokens.ExpiresIn,
                    RefreshTokenExpiresAt = tokens.RefreshTokenExpiresAt,
                    TokenType = tokens.TokenType,
                }
            });
        }

        /// <summary>The refresh token from the request body, or else from the cookie (which also needs the forgery-protection header).</summary>
        private (string? token, bool fromCookie, bool forbidden) ReadRefreshToken(RefreshTokenDto? body)
        {
            if (!string.IsNullOrWhiteSpace(body?.RefreshToken))
                return (body.RefreshToken, false, false);

            var fromCookie = _cookie.Read(Request);
            if (fromCookie == null)
                return (null, false, false);

            return Request.Headers.ContainsKey(CsrfHeader) ? (fromCookie, true, false) : (null, true, true);
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

            return Issue(message, tokens!);
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

            return Issue(message, tokens!);
        }

        /// <summary>Gets a new access token. The refresh token comes from the body, or from the cookie when the body has none.</summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto? request)
        {
            var (token, fromCookie, forbidden) = ReadRefreshToken(request);
            if (forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = $"The {CsrfHeader} header is required" });
            if (token == null)
                return Unauthorized(new { message = "Refresh token is required" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var (success, message, tokens) = await _authenticationService.RefreshTokenAsync(token, userId);

            if (!success)
            {
                // a cookie that no longer works is removed so the browser stops sending it
                if (fromCookie) _cookie.Clear(Response);
                return Unauthorized(new { message });
            }

            return Issue(message, tokens!);
        }

        /// <summary>
        /// Ends this session. Open to anyone holding the refresh token (the access token has often expired by now), and the cookie
        /// is cleared whatever the outcome, so the browser is signed out in every case.
        /// </summary>
        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto? request)
        {
            var (token, _, forbidden) = ReadRefreshToken(request);
            if (forbidden)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = $"The {CsrfHeader} header is required" });

            if (token != null)
                await _authenticationService.RevokeTokenAsync(token);

            if (_cookie.Enabled)
                _cookie.Clear(Response);

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            var shopId = User.FindFirst("shop_id")?.Value;
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
