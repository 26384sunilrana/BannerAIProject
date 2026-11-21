namespace BannerService.Domain.ValueObjects;

/// <summary>Phone numbers in international (E.164) form: a plus sign and 8 to 15 digits.</summary>
public static class PhoneNumbers
{
    public const string DefaultCountryCode = "+91";

    /// <summary>
    /// Turns what a person typed into E.164. Spaces, dashes, dots and brackets are ignored; a leading 00 means "+";
    /// a leading single 0 is a national trunk prefix and is dropped before the country code is added.
    /// </summary>
    public static bool TryNormalize(string? raw, string? defaultCountryCode, out string e164)
    {
        e164 = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim();
        var international = text.StartsWith('+');
        var digits = new string(text.Where(char.IsDigit).ToArray());
        if (digits.Length == 0 || text.Any(c => !(char.IsDigit(c) || c is '+' or ' ' or '-' or '.' or '(' or ')')))
            return false;
        if (text.LastIndexOf('+') > 0)
            return false;

        if (!international && digits.StartsWith("00"))
        {
            international = true;
            digits = digits[2..];
        }

        if (!international)
        {
            var country = new string((defaultCountryCode ?? DefaultCountryCode).Where(char.IsDigit).ToArray());
            if (country.Length == 0)
                return false;
            if (digits.StartsWith('0'))
                digits = digits[1..];
            digits = country + digits;
        }

        if (digits.Length is < 8 or > 15 || digits[0] == '0')
            return false;

        e164 = "+" + digits;
        return true;
    }

    /// <summary>Shows only the last four digits, for logs and screens.</summary>
    public static string Mask(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length <= 4)
            return "****";
        return new string('*', phone.Length - 4) + phone[^4..];
    }
}
