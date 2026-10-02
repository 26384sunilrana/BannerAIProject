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

        /// <summary>
        /// Sends through SMTP when Email:Smtp:Host is set (Port, User, Password, From, EnableSsl alongside it).
        /// Without it nothing can be sent: the message is logged without its body and false is returned.
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            var host = _configuration["Email:Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning("Email not sent to {Email} ({Subject}): Email:Smtp:Host is not configured", toEmail, subject);
                return false;
            }

            try
            {
                using var client = new System.Net.Mail.SmtpClient(host, _configuration.GetValue("Email:Smtp:Port", 587))
                {
                    EnableSsl = _configuration.GetValue("Email:Smtp:EnableSsl", true)
                };

                var user = _configuration["Email:Smtp:User"];
                if (!string.IsNullOrEmpty(user))
                    client.Credentials = new System.Net.NetworkCredential(user, _configuration["Email:Smtp:Password"]);

                var from = _configuration["Email:Smtp:From"] ?? user ?? "noreply@bannerai.local";
                using var message = new System.Net.Mail.MailMessage(from, toEmail, subject, body) { IsBodyHtml = isHtml };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                return false;
            }
        }
    }
}
