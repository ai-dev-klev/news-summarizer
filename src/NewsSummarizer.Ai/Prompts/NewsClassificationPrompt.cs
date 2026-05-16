using System.Text;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Ai.Prompts;

public static class NewsClassificationPrompt
{
    public const string Version = "v1";

    public const string SystemMessage = """
You are a strict news analysis engine for a personalized news summarizer.

Analyze the article and return only valid JSON matching the provided schema.

Rules:
- Use only facts from the title, description and content provided by the user.
- Do not invent facts, dates, numbers, causes or consequences.
- If the article has too little information, keep scores moderate or low and explain uncertainty.
- Scores must be integers from 0 to 100.
- 0 means not relevant at all; 100 means maximally relevant.
- Return no markdown.
- Return no text outside JSON.
""";

    public const string JsonSchema = """
{
  "type": "object",
  "properties": {
    "category": {
      "type": "string",
      "description": "Main article category. Prefer one of: technology, business, science, politics, security, education, health, culture, sports, world, other."
    },
    "importanceScore": {
      "type": "integer",
      "minimum": 0,
      "maximum": 100,
      "description": "How important the article is for a general daily digest."
    },
    "urgencyScore": {
      "type": "integer",
      "minimum": 0,
      "maximum": 100,
      "description": "How time-sensitive the article is."
    },
    "opportunityScore": {
      "type": "integer",
      "minimum": 0,
      "maximum": 100,
      "description": "How useful the article is for deeper analysis, hypotheses, product or business opportunities."
    },
    "summary": {
      "type": "string",
      "description": "Concise summary in 1-2 sentences."
    },
    "reason": {
      "type": "string",
      "description": "Why the article matters."
    },
    "opportunityReason": {
      "type": "string",
      "description": "Why this may or may not be useful for deeper analysis."
    },
    "dailyDigestCandidate": {
      "type": "boolean",
      "description": "Whether the article can be included in a daily digest."
    },
    "opportunityDigestCandidate": {
      "type": "boolean",
      "description": "Whether the article can be included in an opportunity or analysis digest."
    },
    "urgentCandidate": {
      "type": "boolean",
      "description": "Whether the article is urgent enough for a fast notification."
    }
  },
  "required": [
    "category",
    "importanceScore",
    "urgencyScore",
    "opportunityScore",
    "summary",
    "reason",
    "opportunityReason",
    "dailyDigestCandidate",
    "opportunityDigestCandidate",
    "urgentCandidate"
  ],
  "additionalProperties": false
}
""";

    public static string Build(NewsArticle article)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Analyze this news article.");
        builder.AppendLine();
        AppendField(builder, "Title", article.Title);
        AppendField(builder, "Url", article.Url);
        AppendField(builder, "Description", article.Description);
        AppendField(builder, "Language", article.Language);
        AppendField(builder, "PublishedAt", article.PublishedAt?.ToString("O"));
        AppendField(builder, "Content", TrimToLimit(article.Content, 8000));

        return builder.ToString();
    }

    private static void AppendField(StringBuilder builder, string name, string? value)
    {
        builder.AppendLine($"{name}:");
        builder.AppendLine(string.IsNullOrWhiteSpace(value) ? "[not provided]" : value.Trim());
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
