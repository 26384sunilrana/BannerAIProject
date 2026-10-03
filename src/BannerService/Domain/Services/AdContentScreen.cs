namespace BannerService.Domain.Services;

using System.Text.RegularExpressions;

/// <summary>What the screening found in an ad's text.</summary>
public sealed class ContentFindings
{
    /// <summary>Personal or contact details that must never be on an ad. The ad is refused.</summary>
    public List<string> Blocks { get; } = new();

    /// <summary>Health or personal-information wording that may be harmless (a pharmacy) or not. An administrator reviews the ad before it can run.</summary>
    public List<string> Reviews { get; } = new();

    public bool IsClean => Blocks.Count == 0 && Reviews.Count == 0;
}

/// <summary>
/// Looks through the text of an ad for personal information (PII) and health information (PHI). Ads must carry neither: a pattern that is
/// a person's detail (phone, e-mail, card, national id, date of birth) refuses the ad, and wording about health or patients sends it to an
/// administrator for review. This is a safety net with known gaps (names and addresses are not recognised), not a guarantee.
/// </summary>
public static class AdContentScreen
{
    private static readonly Regex Email = new(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}", RegexOptions.Compiled);

    // a run of digits that may be split by spaces, dots, dashes or brackets, optionally starting with +
    private static readonly Regex Number = new(@"(?<![\d])\+?\(?\d[\d\s().\-]{5,}\d(?![\d])", RegexOptions.Compiled);

    private static readonly Regex IsoDate = new(@"^\d{4}-\d{2}-\d{2}$", RegexOptions.Compiled);
    private static readonly Regex YearRange = new(@"^\d{4}\s*-\s*\d{4}$", RegexOptions.Compiled);
    private static readonly Regex Aadhaar = new(@"(?<!\d)\d{4}[\s\-]\d{4}[\s\-]\d{4}(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Ssn = new(@"(?<!\d)\d{3}-\d{2}-\d{4}(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Pan = new(@"\b[A-Z]{5}\d{4}[A-Z]\b", RegexOptions.Compiled);
    private static readonly Regex Birth = new(@"\b(date\s+of\s+birth|d\.?o\.?b\.?)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Health = new(
        @"\b(patients?|diagnos(?:is|ed|es)|medical\s+records?|health\s+records?|prescriptions?|medications?|test\s+results?|lab\s+results?|treatment\s+for|" +
        @"hiv|aids|cancer|diabet(?:es|ic)|pregnan(?:t|cy)|mental\s+health|depression|therapy|rehab|surgery|insurance\s+claims?|mrn|hipaa)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Screens the named pieces of text of one ad. Findings say which piece (for example "headline") they were found in.</summary>
    public static ContentFindings Screen(params (string Field, string? Text)[] pieces)
    {
        var findings = new ContentFindings();
        foreach (var (field, text) in pieces)
        {
            if (string.IsNullOrWhiteSpace(text)) continue;

            if (Email.IsMatch(text)) Add(findings.Blocks, $"The {field} contains an e-mail address.");
            if (Birth.IsMatch(text)) Add(findings.Blocks, $"The {field} mentions a date of birth.");

            var cardFound = false;
            var numbers = Number.Matches(text);
            foreach (Match match in numbers)
            {
                var digits = match.Value.Count(char.IsDigit);
                if (digits is >= 13 and <= 19 && Luhn(match.Value)) cardFound = true;
            }

            // a card number in groups of four also looks like an identity number: report it once, as a card
            if ((Aadhaar.IsMatch(text) && !cardFound) || Ssn.IsMatch(text) || Pan.IsMatch(text)) Add(findings.Blocks, $"The {field} contains what looks like a personal identity number.");

            foreach (Match match in numbers)
            {
                var digits = match.Value.Count(char.IsDigit);
                if (IsoDate.IsMatch(match.Value.Trim()) || YearRange.IsMatch(match.Value.Trim())) continue;

                if (digits is >= 13 and <= 19 && Luhn(match.Value))
                {
                    Add(findings.Blocks, $"The {field} contains what looks like a card number.");
                }
                else if (digits >= 10 || (digits >= 7 && match.Value.Any(c => c is ' ' or '-' or '.' or '(' or ')' or '+')))
                {
                    // an identity number is reported above, not also as a phone number
                    if (!Aadhaar.IsMatch(match.Value) && !Ssn.IsMatch(match.Value)) Add(findings.Blocks, $"The {field} contains what looks like a phone number.");
                }
            }

            foreach (Match match in Health.Matches(text))
                Add(findings.Reviews, $"The {field} uses the health wording \"{match.Value.ToLowerInvariant()}\".");
        }
        return findings;
    }

    private static void Add(List<string> list, string message)
    {
        if (!list.Contains(message)) list.Add(message);
    }

    private static bool Luhn(string text)
    {
        var sum = 0;
        var alternate = false;
        for (var i = text.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(text[i])) continue;
            var d = text[i] - '0';
            if (alternate)
            {
                d *= 2;
                if (d > 9) d -= 9;
            }
            sum += d;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }
}
