using System.Security.Cryptography;
using System.Text;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Services;

public sealed class ArticleNormalizationService
{
    public string NormalizeTitle(string title)
    {
        return string.Join(
            " ",
            title.Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
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
            Fragment = string.Empty,
            Query = RemoveTrackingQuery(uri.Query)
        };

        return builder.Uri.ToString();
    }

    public ArticleDeduplicationKey BuildKey(NewsArticle article)
    {
        return new ArticleDeduplicationKey(
            article.Url,
            article.CanonicalUrl,
            article.NormalizedTitle,
            article.ContentHash,
            article.DedupKey);
    }

    private static string RemoveTrackingQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var trimmed = query.TrimStart('?');

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        var keptParts = trimmed
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part =>
            {
                var key = part.Split('=', 2)[0];

                return !key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(key, "yclid", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(key, "fbclid", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(key, "gclid", StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        return string.Join("&", keptParts);
    }
}