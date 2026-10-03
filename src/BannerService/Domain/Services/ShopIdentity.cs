namespace BannerService.Domain.Services;

using System.Text.RegularExpressions;

/// <summary>
/// What makes a shop the same shop: its name and where it is. Comparing is forgiving (case, spaces and punctuation do not matter) so
/// "12, Main Road." and "12 main road" are one address. A shop with no address has no identity yet and matches nothing.
/// </summary>
public static class ShopIdentity
{
    private static readonly Regex NotAlphanumeric = new(@"[^\p{L}\p{N}]+", RegexOptions.Compiled);

    public static string Normalise(string? text) =>
        string.IsNullOrWhiteSpace(text) ? string.Empty : NotAlphanumeric.Replace(text.Trim().ToLowerInvariant(), " ").Trim();

    public static bool HasAddress(string? address) => Normalise(address).Length > 0;

    /// <summary>Same name, same street address and the same postal code (when both have one).</summary>
    public static bool Same(string? nameA, string? addressA, string? postalA, string? nameB, string? addressB, string? postalB)
    {
        if (!HasAddress(addressA) || !HasAddress(addressB)) return false;
        if (Normalise(nameA) != Normalise(nameB)) return false;
        if (Normalise(addressA) != Normalise(addressB)) return false;

        var a = Normalise(postalA).Replace(" ", string.Empty);
        var b = Normalise(postalB).Replace(" ", string.Empty);
        return a.Length == 0 || b.Length == 0 || a == b;
    }
}
