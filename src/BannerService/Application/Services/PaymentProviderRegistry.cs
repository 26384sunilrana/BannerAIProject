namespace BannerService.Application.Services;

using Domain.Interfaces;

/// <summary>
/// Picks the payment provider named in settings (ADR-004). Current is null when the provider is None or is not fully set up:
/// the application then behaves as before (Renew click, nobody charged).
/// </summary>
public class PaymentProviderRegistry
{
    public PaymentProviderRegistry(IEnumerable<IPaymentProvider> providers, IConfiguration configuration)
    {
        var name = ProviderSettings.PaymentProvider(configuration);
        Current = providers.FirstOrDefault(p => p.IsConfigured && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        DefaultCurrency = ProviderSettings.DefaultCurrency(configuration);
        DefaultCountry = ProviderSettings.DefaultCountry(configuration);
    }

    public IPaymentProvider? Current { get; }

    /// <summary>True when owners pay through a provider. Auto-renew is then ignored (ADR-005).</summary>
    public bool HasRealProvider => Current != null;

    public string DefaultCurrency { get; }

    public string DefaultCountry { get; }
}
