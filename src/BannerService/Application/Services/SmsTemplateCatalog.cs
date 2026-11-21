namespace BannerService.Application.Services;

using Domain.Interfaces;

/// <summary>
/// The registered SMS texts, read from settings: Sms:Templates:&lt;key&gt;:ProviderTemplateId (the DLT id in India), :Text (gaps written as {#var#})
/// and :VariableNames (optional, the provider's names for the gaps in order; default var1, var2, ...).
/// An unknown key is refused: free text is never sent.
/// </summary>
public class SmsTemplateCatalog : ISmsTemplateCatalog
{
    public const string Gap = "{#var#}";

    private readonly IConfiguration _configuration;

    public SmsTemplateCatalog(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public SmsTemplate? Find(string key)
    {
        var section = _configuration.GetSection($"Sms:Templates:{key}");
        var text = section["Text"];
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var names = section.GetSection("VariableNames").Get<string[]>();
        if (names == null || names.Length == 0)
            names = Enumerable.Range(1, CountGaps(text)).Select(i => $"var{i}").ToArray();

        return new SmsTemplate(key, section["ProviderTemplateId"] ?? string.Empty, text, names);
    }

    public string Render(SmsTemplate template, IReadOnlyList<string> values)
    {
        var result = template.Text;
        foreach (var value in values)
        {
            var at = result.IndexOf(Gap, StringComparison.Ordinal);
            if (at < 0) break;
            result = result[..at] + value + result[(at + Gap.Length)..];
        }
        return result;
    }

    private static int CountGaps(string text)
    {
        var count = 0;
        var at = 0;
        while ((at = text.IndexOf(Gap, at, StringComparison.Ordinal)) >= 0)
        {
            count++;
            at += Gap.Length;
        }
        return count;
    }
}
