using BannerService.Domain.Services;
using Xunit;

namespace BannerService.Domain.Tests.Services;

public class AdContentScreenTests
{
    private static ContentFindings Screen(string? headline, string? body = null, string? advertiser = "Olive Cafe") =>
        AdContentScreen.Screen(("advertiser name", advertiser), ("headline", headline), ("text", body));

    [Theory]
    [InlineData("Two for one coffee")]
    [InlineData("Fresh bread every morning, from 7 to 10")]
    [InlineData("10-20% off everything")]
    [InlineData("Open 9-5, Monday to Saturday")]
    [InlineData("Since 1990-2020 we have served you")]
    [InlineData("Sale runs 2030-01-07")]
    [InlineData("Only Rs 1200000 for a flat")]
    [InlineData("Fees from 1,200,000 a year")]
    [InlineData("Shop 24 on Main Road")]
    [InlineData("Win 5 prizes, 100 winners")]
    [InlineData("")]
    public void OrdinaryAdsAreClean(string text) => Assert.True(Screen(text).IsClean, string.Join(" ", Screen(text).Blocks.Concat(Screen(text).Reviews)));

    [Theory]
    [InlineData("Call 9876543210 now")]
    [InlineData("Call +91 98765 43210")]
    [InlineData("Phone (555) 123-4567")]
    [InlineData("Call 555-1234 today")]
    [InlineData("Ring 020 2567 8901")]
    public void PhoneNumbersAreRefused(string text)
    {
        var found = Screen(text);

        Assert.Contains("phone number", Assert.Single(found.Blocks));
        Assert.Contains("headline", found.Blocks[0]);
    }

    [Theory]
    [InlineData("Write to sam@example.com")]
    [InlineData("mail:first.last+ads@sub.example.co.in")]
    public void EmailAddressesAreRefused(string text) => Assert.Contains("e-mail address", Assert.Single(Screen(text).Blocks));

    [Theory]
    [InlineData("Card 4111 1111 1111 1111 accepted")]
    [InlineData("pay with 4111-1111-1111-1111")]
    [InlineData("4111111111111111")]
    public void CardNumbersAreRefused(string text) => Assert.Contains("card number", Assert.Single(Screen(text).Blocks));

    [Fact]
    public void ALongNumberThatIsNotACardIsStillAPhoneNumber() =>
        Assert.Contains("phone number", Assert.Single(Screen("Order 1234567890123456").Blocks));

    [Theory]
    [InlineData("Aadhaar 1234 5678 9012")]
    [InlineData("SSN 123-45-6789")]
    [InlineData("PAN ABCDE1234F")]
    public void IdentityNumbersAreRefused(string text) => Assert.Contains("identity number", Assert.Single(Screen(text).Blocks));

    [Theory]
    [InlineData("DOB 12/05/1980")]
    [InlineData("Date of birth: 1 Jan")]
    public void DatesOfBirthAreRefused(string text) => Assert.Contains("date of birth", Assert.Single(Screen(text).Blocks));

    [Theory]
    [InlineData("Free flu shots for every patient", "patient")]
    [InlineData("Diabetes screening camp", "diabetes")]
    [InlineData("Lab results in one hour", "lab results")]
    [InlineData("Prescription refills made easy", "prescription")]
    [InlineData("HIV testing, no questions", "hiv")]
    [InlineData("Mental Health week", "mental health")]
    public void HealthWordingGoesToReview_NotRefusal(string text, string word)
    {
        var found = Screen(text);

        Assert.Empty(found.Blocks);
        Assert.Contains(found.Reviews, r => r.Contains($"\"{word}\""));
    }

    [Fact]
    public void FindingsNameTheFieldAndAreNotRepeated()
    {
        var found = AdContentScreen.Screen(("headline", "Call 9876543210 or 9123456789"), ("text", "mail a@b.com"), ("advertiser name", "Dr Patient Care Clinic"));

        Assert.Equal(new[] { "The headline contains what looks like a phone number.", "The text contains an e-mail address." }, found.Blocks);
        Assert.Contains("advertiser name", Assert.Single(found.Reviews));
    }

    [Fact]
    public void NullAndBlankPiecesAreSkipped() => Assert.True(AdContentScreen.Screen(("headline", null), ("text", "   ")).IsClean);
}
