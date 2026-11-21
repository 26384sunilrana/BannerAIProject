namespace BannerService.Domain.ValueObjects;

/// <summary>ISO 4217 currency codes: three upper-case letters. Which codes a provider accepts is the provider's business.</summary>
public static class CurrencyCodes
{
    public const string Default = "INR";

    public static bool IsValid(string? code) =>
        code is { Length: 3 } && code.All(c => c is >= 'A' and <= 'Z');

    /// <summary>Upper-cases the code; an empty value becomes the fallback. An invalid code is refused.</summary>
    public static string Normalize(string? code, string fallback = Default)
    {
        var value = string.IsNullOrWhiteSpace(code) ? fallback : code.Trim().ToUpperInvariant();
        if (!IsValid(value))
            throw new ArgumentException($"'{code}' is not a three-letter currency code", nameof(code));
        return value;
    }

    /// <summary>How many digits follow the decimal point in the currency's smallest unit (paise, cents, fils).</summary>
    public static int MinorUnitDigits(string currency) => currency switch
    {
        "JPY" or "KRW" or "VND" or "CLP" or "ISK" or "UGX" or "XAF" or "XOF" or "PYG" => 0,
        "KWD" or "BHD" or "OMR" or "JOD" or "TND" => 3,
        _ => 2,
    };
}

/// <summary>An amount in one currency. Equality is by value. Minor units (paise, cents) are used only at the provider boundary.</summary>
public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Of(decimal amount, string? currency)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "An amount cannot be negative");
        return new Money(amount, CurrencyCodes.Normalize(currency));
    }

    public long ToMinorUnits()
    {
        var factor = (decimal)Math.Pow(10, CurrencyCodes.MinorUnitDigits(Currency));
        return (long)Math.Round(Amount * factor, MidpointRounding.AwayFromZero);
    }

    public static Money FromMinorUnits(long minor, string currency)
    {
        var code = CurrencyCodes.Normalize(currency);
        var factor = (decimal)Math.Pow(10, CurrencyCodes.MinorUnitDigits(code));
        return new Money(minor / factor, code);
    }

    /// <summary>True when both amounts are the same currency and the same number of minor units.</summary>
    public bool Matches(Money other) =>
        string.Equals(Currency, other.Currency, StringComparison.Ordinal) && ToMinorUnits() == other.ToMinorUnits();

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
