using System.Text.Json;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Ai.Parsing;

public sealed class AiResponseParser
{
    public ArticleAiAnalysisResult ParseArticleAnalysis(string rawResponse)
    {
        if (string.IsNullOrWhiteSpace(rawResponse))
        {
            throw new InvalidOperationException("AI response is empty.");
        }

        var json = ExtractJsonObject(rawResponse);

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("AI response JSON root must be an object.");
            }

            return new ArticleAiAnalysisResult(
                Category: ReadRequiredString(root, "category", json),
                ImportanceScore: ReadScore(root, "importanceScore", json),
                UrgencyScore: ReadScore(root, "urgencyScore", json),
                OpportunityScore: ReadScore(root, "opportunityScore", json),
                Summary: ReadRequiredString(root, "summary", json),
                Reason: ReadRequiredString(root, "reason", json),
                OpportunityReason: ReadRequiredString(root, "opportunityReason", json),
                DailyDigestCandidate: ReadRequiredBoolean(root, "dailyDigestCandidate", json),
                OpportunityDigestCandidate: ReadRequiredBoolean(root, "opportunityDigestCandidate", json),
                UrgentCandidate: ReadRequiredBoolean(root, "urgentCandidate", json),
                RawResponseJson: json);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"AI response is not valid JSON. Raw response: {rawResponse}", exception);
        }
    }

    private static string ExtractJsonObject(string rawResponse)
    {
        var text = RemoveMarkdownFence(rawResponse.Trim());

        if (text.StartsWith('{') && text.EndsWith('}'))
        {
            return text;
        }

        var start = text.IndexOf('{');
        if (start < 0)
        {
            throw new InvalidOperationException($"AI response does not contain JSON object. Raw response: {rawResponse}");
        }

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var index = start; index < text.Length; index++)
        {
            var current = text[index];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (current == '\\' && inString)
            {
                escaped = true;
                continue;
            }

            if (current == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (current == '{')
            {
                depth++;
            }
            else if (current == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return text[start..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException($"AI response contains incomplete JSON object. Raw response: {rawResponse}");
    }

    private static string RemoveMarkdownFence(string text)
    {
        if (!text.StartsWith("```", StringComparison.Ordinal))
        {
            return text;
        }

        var lines = text.Split('\n');

        if (lines.Length >= 2 && lines[^1].Trim().StartsWith("```", StringComparison.Ordinal))
        {
            return string.Join('\n', lines.Skip(1).Take(lines.Length - 2)).Trim();
        }

        return text;
    }

    private static string ReadRequiredString(JsonElement root, string propertyName, string rawJson)
    {
        var element = ReadRequiredProperty(root, propertyName, rawJson);

        if (element.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"AI response property '{propertyName}' must be a string. Raw JSON: {rawJson}");
        }

        var value = element.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"AI response property '{propertyName}' must not be empty. Raw JSON: {rawJson}");
        }

        return value;
    }

    private static int ReadScore(JsonElement root, string propertyName, string rawJson)
    {
        var element = ReadRequiredProperty(root, propertyName, rawJson);
        int value;

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            value = number;
        }
        else if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
        {
            value = parsed;
        }
        else
        {
            throw new InvalidOperationException($"AI response property '{propertyName}' must be an integer. Raw JSON: {rawJson}");
        }

        return Math.Clamp(value, 0, 100);
    }

    private static bool ReadRequiredBoolean(JsonElement root, string propertyName, string rawJson)
    {
        var element = ReadRequiredProperty(root, propertyName, rawJson);

        if (element.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"AI response property '{propertyName}' must be boolean. Raw JSON: {rawJson}");
    }

    private static JsonElement ReadRequiredProperty(JsonElement root, string propertyName, string rawJson)
    {
        if (!root.TryGetProperty(propertyName, out var element))
        {
            throw new InvalidOperationException($"AI response does not contain required property '{propertyName}'. Raw JSON: {rawJson}");
        }

        return element;
    }
}
