namespace BannerService.Infrastructure.Security
{
    /// <summary>
    /// Keeps the long-lived refresh token in an HttpOnly cookie so scripts on the page can never read it. The short-lived access
    /// token stays in the page's memory. Settings (Auth:Cookie): Enabled (default true), Name, SameSite (Lax, Strict or None),
    /// Secure (default: only outside development), Domain (optional, for a web app and API on sibling subdomains).
    /// </summary>
    public interface IRefreshCookie
    {
        /// <summary>When false the refresh token is returned in the response body instead (for non-browser clients).</summary>
        bool Enabled { get; }

        void Write(HttpResponse response, string refreshToken, DateTime expiresAtUtc);
        void Clear(HttpResponse response);
        string? Read(HttpRequest request);
    }

    public class RefreshCookie : IRefreshCookie
    {
        public const string Path = "/api/authentication";

        private readonly string _name;
        private readonly SameSiteMode _sameSite;
        private readonly bool _secure;
        private readonly string? _domain;

        public RefreshCookie(IConfiguration configuration, IHostEnvironment environment)
        {
            Enabled = configuration.GetValue("Auth:Cookie:Enabled", true);
            _name = configuration["Auth:Cookie:Name"] ?? "banner_refresh";
            _sameSite = Enum.TryParse<SameSiteMode>(configuration["Auth:Cookie:SameSite"], true, out var mode) ? mode : SameSiteMode.Lax;
            _secure = configuration.GetValue("Auth:Cookie:Secure", environment.IsProduction());
            _domain = configuration["Auth:Cookie:Domain"];

            // browsers refuse SameSite=None without Secure
            if (_sameSite == SameSiteMode.None && !_secure)
                throw new InvalidOperationException("Auth:Cookie:SameSite=None needs Auth:Cookie:Secure=true");
        }

        public bool Enabled { get; }

        public void Write(HttpResponse response, string refreshToken, DateTime expiresAtUtc) =>
            response.Cookies.Append(_name, refreshToken, Options(expiresAtUtc));

        public void Clear(HttpResponse response) =>
            response.Cookies.Delete(_name, Options(null));

        public string? Read(HttpRequest request) =>
            request.Cookies.TryGetValue(_name, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

        private CookieOptions Options(DateTime? expiresAtUtc)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = _secure,
                SameSite = _sameSite,
                Path = Path,
                IsEssential = true,
            };
            if (expiresAtUtc.HasValue) options.Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc.Value, DateTimeKind.Utc));
            if (!string.IsNullOrWhiteSpace(_domain)) options.Domain = _domain;
            return options;
        }
    }
}
