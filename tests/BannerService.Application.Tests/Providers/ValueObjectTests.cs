using Xunit;
using BannerService.Application.Services;
using BannerService.Domain.Interfaces;
using BannerService.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;

namespace BannerService.Application.Tests.Providers;

public class MoneyTests
{
    [Theory]
    [InlineData(499.50, "INR", 49950)]
    [InlineData(1, "USD", 100)]
    [InlineData(1000, "JPY", 1000)]    // no minor unit
    [InlineData(1.234, "KWD", 1234)]   // three digits
    public void MinorUnits_FollowTheCurrency(double amount, string currency, long expected) =>
        Assert.Equal(expected, Money.Of((decimal)amount, currency).ToMinorUnits());

    [Fact]
    public void FromMinorUnits_RoundTrips()
    {
        var money = Money.FromMinorUnits(49950, "inr");
        Assert.Equal(499.50m, money.Amount);
        Assert.Equal("INR", money.Currency);
    }

    [Fact]
    public void Matches_NeedsTheSameCurrencyAndTheSameMinorUnits()
    {
        Assert.True(Money.Of(10m, "INR").Matches(Money.Of(10.00m, "INR")));
        Assert.False(Money.Of(10m, "INR").Matches(Money.Of(10m, "USD")));
        Assert.False(Money.Of(10m, "INR").Matches(Money.Of(10.01m, "INR")));
    }

    [Theory]
    [InlineData("RS")]
    [InlineData("INRX")]
    [InlineData("12A")]
    public void AnInvalidCurrency_IsRefused(string code) =>
        Assert.Throws<ArgumentException>(() => CurrencyCodes.Normalize(code));

    [Fact]
    public void AnEmptyCurrency_BecomesTheFallback() => Assert.Equal("INR", CurrencyCodes.Normalize(null));

    [Fact]
    public void ANegativeAmount_IsRefused() => Assert.Throws<ArgumentOutOfRangeException>(() => Money.Of(-1m, "INR"));
}

public class PhoneNumberTests
{
    [Theory]
    [InlineData("9876543210", "+91", "+919876543210")]
    [InlineData("09876543210", "+91", "+919876543210")]          // national trunk zero dropped
    [InlineData("98765 43210", "+91", "+919876543210")]
    [InlineData("+91 98765-43210", "+91", "+919876543210")]
    [InlineData("0091 9876543210", "+91", "+919876543210")]
    [InlineData("(415) 555-0100", "+1", "+14155550100")]          // another country by setting
    [InlineData("+44 20 7946 0958", "+91", "+442079460958")]      // already international: default ignored
    public void Normalises(string raw, string defaultCode, string expected)
    {
        Assert.True(PhoneNumbers.TryNormalize(raw, defaultCode, out var e164));
        Assert.Equal(expected, e164);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("98765abc10")]
    [InlineData("+91+9876543210")]
    [InlineData("+0123456789")]
    [InlineData("9999999999999999999")]
    public void Refuses(string raw) => Assert.False(PhoneNumbers.TryNormalize(raw, "+91", out _));

    [Fact]
    public void Mask_ShowsOnlyTheLastFourDigits() => Assert.Equal("*********3210", PhoneNumbers.Mask("+919876543210"));

    [Fact]
    public void Mask_HidesShortValuesCompletely() => Assert.Equal("****", PhoneNumbers.Mask("123"));
}

public class ProviderSettingsTests
{
    private static IConfiguration Config(params (string key, string? value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.key, v => v.value)).Build();

    [Fact]
    public void EverythingEmpty_MeansOff()
    {
        var config = Config();
        Assert.Equal("None", ProviderSettings.PaymentProvider(config));
        Assert.Equal("None", ProviderSettings.SmsProvider(config));
        Assert.False(ProviderSettings.PaymentsConfigured(config));
        Assert.False(ProviderSettings.SmsConfigured(config));
        Assert.False(ProviderSettings.EmailConfigured(config));
        Assert.Empty(ProviderSettings.Validate(config, production: false));
    }

    [Fact]
    public void IndiaIsTheDefault_ButEachIsASetting()
    {
        Assert.Equal("INR", ProviderSettings.DefaultCurrency(Config()));
        Assert.Equal("IN", ProviderSettings.DefaultCountry(Config()));
        Assert.Equal("+91", ProviderSettings.DefaultPhoneCountryCode(Config()));

        var other = Config(("Payments:DefaultCurrency", "usd"), ("Payments:DefaultCountry", "us"), ("Sms:DefaultCountryCode", "+1"));
        Assert.Equal("USD", ProviderSettings.DefaultCurrency(other));
        Assert.Equal("US", ProviderSettings.DefaultCountry(other));
        Assert.Equal("+1", ProviderSettings.DefaultPhoneCountryCode(other));
    }

    [Fact]
    public void ANamedProviderWithMissingValues_IsAProblemThatNamesTheSettingButNotAValue()
    {
        var config = Config(("Payments:Provider", "Razorpay"), ("Payments:Razorpay:KeyId", "rzp_test_secretvalue"));

        var problems = ProviderSettings.Validate(config, production: false);

        Assert.Contains(problems, p => p.Contains("Payments:Razorpay:KeySecret"));
        Assert.Contains(problems, p => p.Contains("Payments:Razorpay:WebhookSecret"));
        Assert.DoesNotContain(problems, p => p.Contains("secretvalue"));
        Assert.False(ProviderSettings.PaymentsConfigured(config));
    }

    [Fact]
    public void AFullyFilledProvider_IsConfigured()
    {
        var config = Config(("Payments:Provider", "Razorpay"), ("Payments:Razorpay:KeyId", "a"), ("Payments:Razorpay:KeySecret", "b"), ("Payments:Razorpay:WebhookSecret", "c"));
        Assert.True(ProviderSettings.PaymentsConfigured(config));
        Assert.Empty(ProviderSettings.Validate(config, production: false));
    }

    [Fact]
    public void AnUnsupportedProvider_IsAProblem()
    {
        Assert.Contains(ProviderSettings.Validate(Config(("Payments:Provider", "Stripe")), false), p => p.Contains("not supported"));
        Assert.Contains(ProviderSettings.Validate(Config(("Sms:Provider", "Twilio")), false), p => p.Contains("not supported"));
    }

    [Fact]
    public void Sms_NeedsItsAuthKey()
    {
        var config = Config(("Sms:Provider", "Msg91"));
        Assert.Contains(ProviderSettings.Validate(config, false), p => p.Contains("Sms:Msg91:AuthKey"));
        Assert.False(ProviderSettings.SmsConfigured(config));
        Assert.True(ProviderSettings.SmsConfigured(Config(("Sms:Provider", "Msg91"), ("Sms:Msg91:AuthKey", "k"))));
    }

    [Theory]
    [InlineData("http://shop.example.com", true)]
    [InlineData("", true)]
    [InlineData("https://shop.example.com", false)]
    public void InProduction_AppUrlMustBeHttps(string appUrl, bool problem)
    {
        var problems = ProviderSettings.Validate(Config(("AppUrl", appUrl)), production: true);
        Assert.Equal(problem, problems.Any(p => p.Contains("AppUrl")));
    }

    [Fact]
    public void OutsideProduction_AppUrlIsNotChecked() =>
        Assert.Empty(ProviderSettings.Validate(Config(("AppUrl", "http://localhost:3000")), production: false));

    [Fact]
    public void AnInvalidDefaultCurrency_IsAProblem() =>
        Assert.Contains(ProviderSettings.Validate(Config(("Payments:DefaultCurrency", "RUPEES")), false), p => p.Contains("DefaultCurrency"));
}

public class SmsTemplateCatalogTests
{
    private static SmsTemplateCatalog Catalog(params (string key, string? value)[] values) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(v => v.key, v => v.value)).Build());

    [Fact]
    public void AnUnknownKey_IsNotFound() => Assert.Null(Catalog().Find("nothing"));

    [Fact]
    public void ReadsTheTemplate_AndNamesTheGapsVar1Var2ByDefault()
    {
        var catalog = Catalog(
            ("Sms:Templates:renewal-reminder:ProviderTemplateId", "tpl-1"),
            ("Sms:Templates:renewal-reminder:Text", "Plan {#var#} ends on {#var#}."));

        var template = catalog.Find("renewal-reminder")!;

        Assert.Equal("tpl-1", template.ProviderTemplateId);
        Assert.Equal(new[] { "var1", "var2" }, template.VariableNames);
    }

    [Fact]
    public void ConfiguredVariableNames_Win()
    {
        var catalog = Catalog(
            ("Sms:Templates:t:Text", "Hi {#var#}"),
            ("Sms:Templates:t:VariableNames:0", "VAR_A"));
        Assert.Equal(new[] { "VAR_A" }, catalog.Find("t")!.VariableNames);
    }

    [Fact]
    public void Render_PutsTheValuesIntoTheGaps()
    {
        var catalog = Catalog(("Sms:Templates:t:Text", "Plan {#var#} ends on {#var#}."));
        var template = catalog.Find("t")!;
        Assert.Equal("Plan Gold ends on 1 Dec 2026.", catalog.Render(template, new[] { "Gold", "1 Dec 2026" }));
    }

    [Fact]
    public void Render_WithFewerValues_LeavesTheRestOfTheGaps()
    {
        var catalog = Catalog(("Sms:Templates:t:Text", "A {#var#} B {#var#}"));
        Assert.Equal("A x B {#var#}", catalog.Render(catalog.Find("t")!, new[] { "x" }));
    }
}

public class RecoveryTokensTests
{
    [Fact]
    public void Tokens_AreLongRandomAndSafeInALink()
    {
        var a = RecoveryTokens.NewToken();
        var b = RecoveryTokens.NewToken();

        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 43);
        Assert.Matches("^[A-Za-z0-9_-]+$", a);
    }

    [Fact]
    public void TheHash_IsStable_AndIsNotTheToken()
    {
        var token = RecoveryTokens.NewToken();
        Assert.Equal(RecoveryTokens.Hash(token), RecoveryTokens.Hash(" " + token + " "));
        Assert.NotEqual(token, RecoveryTokens.Hash(token));
        Assert.Equal(64, RecoveryTokens.Hash(token).Length);
    }
}
