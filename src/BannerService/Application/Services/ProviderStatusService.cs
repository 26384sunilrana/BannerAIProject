namespace BannerService.Application.Services;

using Domain.Interfaces;
using Domain.ValueObjects;

public class ProviderStatusDto
{
    /// <summary>payments, sms or email</summary>
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool Configured { get; set; }

    /// <summary>Names of settings that are empty (never their values).</summary>
    public List<string> Missing { get; set; } = new();
}

public class ProviderTestResult
{
    public bool Sent { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// What the administrator's Providers screen shows and does: whether payments, SMS and e-mail are set up, and test buttons.
/// It reports only yes or no and the names of missing settings, never a key, secret or address.
/// </summary>
public class ProviderStatusService
{
    private readonly IConfiguration _configuration;
    private readonly PaymentProviderRegistry _payments;
    private readonly ISmsSender _sms;
    private readonly ISmsTemplateCatalog _templates;
    private readonly IEmailService _email;

    public ProviderStatusService(IConfiguration configuration, PaymentProviderRegistry payments, ISmsSender sms, ISmsTemplateCatalog templates, IEmailService email)
    {
        _configuration = configuration;
        _payments = payments;
        _sms = sms;
        _templates = templates;
        _email = email;
    }

    public List<ProviderStatusDto> GetStatus() => new()
    {
        new ProviderStatusDto
        {
            Name = "payments",
            Provider = ProviderSettings.PaymentProvider(_configuration),
            Configured = _payments.HasRealProvider,
            Missing = ProviderSettings.MissingPaymentSettings(_configuration).ToList(),
        },
        new ProviderStatusDto
        {
            Name = "sms",
            Provider = ProviderSettings.SmsProvider(_configuration),
            Configured = ProviderSettings.SmsConfigured(_configuration),
            Missing = ProviderSettings.MissingSmsSettings(_configuration).ToList(),
        },
        new ProviderStatusDto
        {
            Name = "email",
            Provider = ProviderSettings.EmailConfigured(_configuration) ? "Smtp" : ProviderSettings.None,
            Configured = ProviderSettings.EmailConfigured(_configuration),
            Missing = ProviderSettings.EmailConfigured(_configuration) ? new List<string>() : new List<string> { "Email:Smtp:Host" },
        },
    };

    public async Task<ProviderTestResult> SendTestEmailAsync(string toEmail)
    {
        if (!ProviderSettings.EmailConfigured(_configuration))
            return new ProviderTestResult { Sent = false, Reason = "E-mail is not set up (Email:Smtp:Host is empty)" };

        var sent = await _email.SendEmailAsync(toEmail, "Banner AI test e-mail", "<p>This is a test message from Banner AI. E-mail is working.</p>", isHtml: true);
        return new ProviderTestResult { Sent = sent, Reason = sent ? null : "The mail server did not accept the message. See the log." };
    }

    /// <summary>The test uses the renewal-reminder template with sample values, never free text.</summary>
    public async Task<ProviderTestResult> SendTestSmsAsync(string phone)
    {
        if (!ProviderSettings.SmsConfigured(_configuration))
            return new ProviderTestResult { Sent = false, Reason = "SMS is not set up" };

        var defaultCode = ProviderSettings.DefaultPhoneCountryCode(_configuration);
        if (!PhoneNumbers.TryNormalize(phone, defaultCode, out var e164))
            return new ProviderTestResult { Sent = false, Reason = "That is not a valid phone number" };

        if (_templates.Find(SmsTemplateKeys.RenewalReminder) is not { } template || string.IsNullOrWhiteSpace(template.ProviderTemplateId))
            return new ProviderTestResult { Sent = false, Reason = $"Template {SmsTemplateKeys.RenewalReminder} has no ProviderTemplateId" };

        var sent = await _sms.SendTemplateAsync(e164, template.Key, new[] { "Test plan", DateTime.UtcNow.AddDays(7).ToString("d MMM yyyy") });
        return new ProviderTestResult { Sent = sent, Reason = sent ? null : "The SMS provider did not accept the message. See the log." };
    }
}
