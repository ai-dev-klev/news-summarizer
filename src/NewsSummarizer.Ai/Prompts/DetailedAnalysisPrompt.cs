using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Ai.Prompts;

public static class DetailedAnalysisPrompt
{
    public const string Version = "v1";

    public const string SystemMessage = """
You are a careful news analyst.

Write a useful detailed analysis of the article.

Rules:
- Use only facts from the article and user preferences.
- Clearly separate facts from hypotheses.
- Do not invent unsupported details.
- Keep the answer concise but useful.
- If the article has too little information, explicitly say what is uncertain.
""";

    public static string Build(NewsArticle article, UserPreferences preferences)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Prepare detailed analysis for this news article.");
        builder.AppendLine();

        builder.AppendLine("Required sections:");
        builder.AppendLine("1. What happened");
        builder.AppendLine("2. Why it matters");
        builder.AppendLine("3. Possible consequences");
        builder.AppendLine("4. Opportunity or hypothesis");
        builder.AppendLine("5. What to check next");
        builder.AppendLine("6. Risks");
        builder.AppendLine();

        builder.AppendLine("User preferences:");
        AppendList(builder, "EnabledCategories", preferences.EnabledCategories);
        AppendList(builder, "UrgentTopics", preferences.UrgentTopics);
        AppendField(builder, "ImportantTopicsText", preferences.ImportantTopicsText);
        AppendField(builder, "ExcludedTopicsText", preferences.ExcludedTopicsText);
        builder.AppendLine();

        builder.AppendLine("Article:");
        AppendField(builder, "Title", article.Title);
        AppendField(builder, "Url", article.Url);
        AppendField(builder, "Description", article.Description);
        AppendField(builder, "Language", article.Language);
        AppendField(builder, "PublishedAt", article.PublishedAt?.ToString("O"));
        AppendField(builder, "Content", TrimToLimit(article.Content, 10000));

        return builder.ToString();
    }

    private static void AppendField(StringBuilder builder, string name, string? value)
    {
        builder.AppendLine($"{name}:");
        builder.AppendLine(string.IsNullOrWhiteSpace(value) ? "[not provided]" : value.Trim());
        builder.AppendLine();
    }

    private static void AppendList(StringBuilder builder, string name, IReadOnlyCollection<string> values)
    {
        builder.AppendLine($"{name}:");

        var normalized = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();

        if (normalized.Length == 0)
        {
            builder.AppendLine("[not provided]");
        }
        else
        {
            foreach (var value in normalized)
            {
                builder.AppendLine("- " + value);
            }
        }

        builder.AppendLine();
    }

    private static string? TrimToLimit(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength] + "\n[content truncated]";
    }
}
