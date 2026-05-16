using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace NewsSummarizer.Core.Services;

public sealed class ArticleNormalizationService
{
    public string NormalizeTitle(string title)
    {
        var normalized = title.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"\s+", " ");
        return normalized;
    }

    public string? ComputeContentHash(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var normalized = content.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public string? BuildCanonicalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return url.Trim();
        }

        var builder = new UriBuilder(uri)
        {
            Fragment = string.Empty
        };

        var query = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !x.StartsWith("utm_", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.StartsWith("yclid=", StringComparison.OrdinalIgnoreCase))
            .Where(x => !x.StartsWith("gclid=", StringComparison.OrdinalIgnoreCase));

        builder.Query = string.Join("&", query);
        return builder.Uri.ToString();
    }
}