namespace BannerService.Domain.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendVerificationEmailAsync(string email, string verificationToken, string userName);
        Task<bool> SendPasswordResetEmailAsync(string email, string resetToken, string userName);
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false);
    }
}
