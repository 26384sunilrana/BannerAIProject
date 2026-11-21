namespace BannerService.Infrastructure.Services;

using System.Text;
using System.Text.Json;
using Domain.Interfaces;
using Domain.ValueObjects;
using Monitoring;

/// <summary>
/// MSG91 behind the SMS contract (ADR-004), through its Flow API, which sends a registered (DLT) template. Settings: Sms:Msg91:AuthKey and
/// optionally ApiBase; each template's id and text are in Sms:Templates. Free text is never sent. Written to MSG91's published API and
/// tested with a fake HTTP handler; the first live check belongs to bolt 044.
/// </summary>
public class Msg91SmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly ISmsTemplateCatalog _templates;
    private readonly string _authKey;
    private readonly string _apiBase;
    private readonly string _countryCode;
    private readonly ILogger<Msg91SmsSender> _logger;

    public Msg91SmsSender(HttpClient http, IConfiguration configuration, ISmsTemplateCatalog templates, ILogger<Msg91SmsSender> logger)
    {
        _http = http;
        _templates = templates;
        _logger = logger;
        _authKey = configuration["Sms:Msg91:AuthKey"] ?? string.Empty;
        var apiBase = configuration["Sms:Msg91:ApiBase"];
        _apiBase = (string.IsNullOrWhiteSpace(apiBase) ? "https://control.msg91.com" : apiBase).TrimEnd('/');
        var code = configuration["Sms:DefaultCountryCode"];
        _countryCode = string.IsNullOrWhiteSpace(code) ? PhoneNumbers.DefaultCountryCode : code;
        if (_http.Timeout == Timeout.InfiniteTimeSpan || _http.Timeout > TimeSpan.FromSeconds(10))
            _http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>Free text cannot be sent through a registered-template provider.</summary>
    public Task<bool> SendAsync(string phoneNumber, string message)
    {
        _logger.LogWarning("SMS to {Phone} was not sent: the provider only sends registered templates", PhoneNumbers.Mask(phoneNumber));
        PaymentMetrics.SmsSent.WithLabels("refused").Inc();
        return Task.FromResult(false);
    }

    public async Task<bool> SendTemplateAsync(string phoneNumber, string templateKey, IReadOnlyList<string> values)
    {
        var template = _templates.Find(templateKey);
        if (template == null || string.IsNullOrWhiteSpace(template.ProviderTemplateId))
        {
            _logger.LogWarning("SMS template {Template} is not set up (Sms:Templates:{Template}:ProviderTemplateId); nothing was sent", templateKey, templateKey);
            PaymentMetrics.SmsSent.WithLabels("no_template").Inc();
            return false;
        }

        if (!PhoneNumbers.TryNormalize(phoneNumber, _countryCode, out var e164))
        {
            _logger.LogWarning("SMS to {Phone} was not sent: not a valid phone number", PhoneNumbers.Mask(phoneNumber));
            PaymentMetrics.SmsSent.WithLabels("bad_number").Inc();
            return false;
        }

        var recipient = new Dictionary<string, string> { ["mobiles"] = e164.TrimStart('+') };
        for (var i = 0; i < template.VariableNames.Count && i < values.Count; i++)
            recipient[template.VariableNames[i]] = values[i];

        var body = JsonSerializer.Serialize(new
        {
            template_id = template.ProviderTemplateId,
            short_url = "0",
            recipients = new[] { recipient },
        });

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{_apiBase}/api/v5/flow")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            message.Headers.TryAddWithoutValidation("authkey", _authKey);

            using var response = await _http.SendAsync(message);
            var json = await response.Content.ReadAsStringAsync();
            var accepted = response.IsSuccessStatusCode && IsSuccess(json);

            _logger.LogInformation("SMS {Template} to {Phone}: {Result}", templateKey, PhoneNumbers.Mask(e164), accepted ? "accepted" : $"refused (HTTP {(int)response.StatusCode})");
            PaymentMetrics.SmsSent.WithLabels(accepted ? "sent" : "refused").Inc();
            return accepted;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "SMS to {Phone} could not be sent: the provider could not be reached", PhoneNumbers.Mask(e164));
            PaymentMetrics.SmsSent.WithLabels("unreachable").Inc();
            return false;
        }
    }

    private static bool IsSuccess(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("type", out var type) && string.Equals(type.GetString(), "success", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
