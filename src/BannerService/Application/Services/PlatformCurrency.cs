namespace BannerService.Application.Services;

using Domain.ValueObjects;

/// <summary>
/// The currency the platform prices in: the setting <c>Payments:DefaultCurrency</c>, INR when it is empty or not a three-letter code.
/// An ad rate takes it whenever the rate is saved; an ad keeps the currency of the rate that priced it, so an amount never changes meaning.
/// </summary>
public sealed class PlatformCurrency
{
    public PlatformCurrency(string code) => Code = CurrencyCodes.Normalize(code);

    public PlatformCurrency(IConfiguration configuration) : this(ProviderSettings.DefaultCurrency(configuration)) { }

    /// <summary>Three upper-case letters, for example INR.</summary>
    public string Code { get; }

    /// <summary>INR, for code that is built without the setting (tests).</summary>
    public static PlatformCurrency Default { get; } = new(CurrencyCodes.Default);
}
