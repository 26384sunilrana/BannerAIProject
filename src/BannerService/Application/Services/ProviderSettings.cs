namespace BannerService.Application.Services;

using Domain.ValueObjects;

/// <summary>
/// What the settings say about payments, SMS and e-mail (ADR-004, ADR-008). An empty setting always means "off".
/// Settings: Payments:Provider (None | Razorpay), Payments:DefaultCurrency, Payments:DefaultCountry, Payments:Razorpay:*;
/// Sms:Provider (None | Msg91), Sms:DefaultCountryCode, Sms:Msg91:*, Sms:Templates:*; Email:Smtp:*.
/// </summary>
public static class ProviderSettings
{
    public const string None = "None";
    public const string Razorpay = "Razorpay";
    public const string Msg91 = "Msg91";

    public static readonly string[] SupportedPaymentProviders = { None, Razorpay };
    public static readonly string[] SupportedSmsProviders = { None, Msg91 };

    public static string PaymentProvider(IConfiguration config) => Name(config["Payments:Provider"]);

    public static string SmsProvider(IConfiguration config) => Name(config["Sms:Provider"]);

    public static string DefaultCurrency(IConfiguration config) =>
        CurrencyCodes.IsValid(config["Payments:DefaultCurrency"]?.Trim().ToUpperInvariant())
            ? config["Payments:DefaultCurrency"]!.Trim().ToUpperInvariant()
            : CurrencyCodes.Default;

    public static string DefaultCountry(IConfiguration config)
    {
        var value = config["Payments:DefaultCountry"]?.Trim().ToUpperInvariant();
        return value is { Length: 2 } ? value : "IN";
    }

    public static string DefaultPhoneCountryCode(IConfiguration config)
    {
        var value = config["Sms:DefaultCountryCode"]?.Trim();
        return string.IsNullOrEmpty(value) ? PhoneNumbers.DefaultCountryCode : value;
    }

    /// <summary>The settings a payment provider needs that have no value. Empty when the provider is None or fully set up.</summary>
    public static IReadOnlyList<string> MissingPaymentSettings(IConfiguration config) =>
        PaymentProvider(config) switch
        {
            Razorpay => Missing(config, "Payments:Razorpay:KeyId", "Payments:Razorpay:KeySecret", "Payments:Razorpay:WebhookSecret"),
            _ => Array.Empty<string>(),
        };

    public static IReadOnlyList<string> MissingSmsSettings(IConfiguration config) =>
        SmsProvider(config) switch
        {
            Msg91 => Missing(config, "Sms:Msg91:AuthKey"),
            _ => Array.Empty<string>(),
        };

    public static bool PaymentsConfigured(IConfiguration config) =>
        PaymentProvider(config) != None && MissingPaymentSettings(config).Count == 0;

    public static bool SmsConfigured(IConfiguration config) =>
        SmsProvider(config) != None && MissingSmsSettings(config).Count == 0;

    public static bool EmailConfigured(IConfiguration config) => !string.IsNullOrWhiteSpace(config["Email:Smtp:Host"]);

    /// <summary>
    /// Problems with the settings, as messages that name the setting and never its value. In production the application refuses to start
    /// when this is not empty; elsewhere it only warns and the provider stays off.
    /// </summary>
    public static IReadOnlyList<string> Validate(IConfiguration config, bool production)
    {
        var problems = new List<string>();

        var payments = PaymentProvider(config);
        if (!SupportedPaymentProviders.Contains(payments))
            problems.Add($"Payments:Provider '{payments}' is not supported (use {string.Join(" or ", SupportedPaymentProviders)})");
        foreach (var setting in MissingPaymentSettings(config))
            problems.Add($"{setting} is empty but Payments:Provider is {payments}");

        var sms = SmsProvider(config);
        if (!SupportedSmsProviders.Contains(sms))
            problems.Add($"Sms:Provider '{sms}' is not supported (use {string.Join(" or ", SupportedSmsProviders)})");
        foreach (var setting in MissingSmsSettings(config))
            problems.Add($"{setting} is empty but Sms:Provider is {sms}");

        var currency = config["Payments:DefaultCurrency"];
        if (!string.IsNullOrWhiteSpace(currency) && !CurrencyCodes.IsValid(currency.Trim().ToUpperInvariant()))
            problems.Add("Payments:DefaultCurrency is not a three-letter currency code");

        if (production)
        {
            var appUrl = config["AppUrl"];
            if (!Uri.TryCreate(appUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                problems.Add("AppUrl must be an https address in production (it is used in the links in e-mails)");
        }

        return problems;
    }

    private static string Name(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return None;
        var trimmed = value.Trim();
        return trimmed.Equals(None, StringComparison.OrdinalIgnoreCase) ? None : trimmed;
    }

    private static IReadOnlyList<string> Missing(IConfiguration config, params string[] keys) =>
        keys.Where(k => string.IsNullOrWhiteSpace(config[k])).ToList();
}
