
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Core.Services;

public static class ArticleEmbeddingTextBuilder
{
    private const int DefaultMaxLength = 6000;

    public static string Build(NewsArticle article, int maxLength = DefaultMaxLength)
    {
        ArgumentNullException.ThrowIfNull(article);

        var builder = new StringBuilder();

        Append(builder, "Title", article.Title);
        Append(builder, "Description", article.Description);
        Append(builder, "Content", article.Content);

        var text = NormalizeText(builder.ToString());

        return text.Length <= maxLength
            ? text
            : text[..maxLength];
    }

    public static string ComputeTextHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void Append(StringBuilder builder, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.Append(label);
        builder.Append(": ");
        builder.AppendLine(value.Trim());
        builder.AppendLine();
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(value);
        var withoutTags = Regex.Replace(decoded, "<.*?>", " ");
        var normalizedWhitespace = Regex.Replace(withoutTags, "\\s+", " ");

        return normalizedWhitespace.Trim();
    }
}
