namespace BannerService.Application.Services
{
    using Domain.Interfaces;
    using Serilog;

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string verificationToken, string userName)
        {
            var verificationLink = _configuration["AppUrl"] ?? "http://localhost:3000";
            var link = $"{verificationLink}/verify-email?token={verificationToken}";

            var body = $@"
                <h2>Email Verification</h2>
                <p>Hello {userName},</p>
                <p>Thank you for registering. Please verify your email by clicking the link below:</p>
                <a href='{link}'>Verify Email</a>
                <p>This link expires in 1 hour.</p>
            ";

            return await SendEmailAsync(email, "Email Verification", body, isHtml: true);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName)
        {
            var resetLink = _configuration["AppUrl"] ?? "http://localhost:3000";
            var link = $"{resetLink}/reset-password?token={resetToken}";

            var body = $@"
                <h2>Password Reset Request</h2>
                <p>Hello {userName},</p>
                <p>You requested a password reset. Click the link below to reset your password:</p>
                <a href='{link}'>Reset Password</a>
                <p>This link expires in 1 hour. If you didn't request this, ignore this email.</p>
            ";

            return await SendEmailAsync(email, "Password Reset", body, isHtml: true);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            try
            {
                // Log email instead of sending in development mode
                _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
                _logger.LogDebug("Email body: {Body}", body);

                // TODO: Implement SMTP configuration for production
                // For now, we log the email to console/file for testing
                Console.WriteLine($"[EMAIL] To: {toEmail}");
                Console.WriteLine($"[EMAIL] Subject: {subject}");
                Console.WriteLine($"[EMAIL] Body: {body}");

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                return false;
            }
        }
    }
}
