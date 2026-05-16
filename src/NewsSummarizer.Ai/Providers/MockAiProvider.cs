using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Ai.Providers;

public sealed class MockAiProvider : IAiProvider, IAiProviderInfo
{
    public AiProviderType Provider => AiProviderType.Mock;
    public string Model => "mock-ai";
    public string PromptVersion => "v1";

    public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(
        NewsArticle article,
        CancellationToken cancellationToken)
    {
        var isUrgent = ContainsAny(article.Title, "urgent", "war", "pandemic", "crisis");
        var isOpportunity = ContainsAny(article.Title, "startup", "ai", "technology", "market");

        var result = new ArticleAiAnalysisResult(
            Category: article.Language == "ru" ? "general" : "general",
            ImportanceScore: isUrgent ? 90 : 60,
            UrgencyScore: isUrgent ? 85 : 15,
            OpportunityScore: isOpportunity ? 80 : 40,
            Summary: $"Mock summary for: {article.Title}",
            Reason: "Mock reason for local pipeline testing.",
            OpportunityReason: isOpportunity ? "Mock opportunity signal." : "No strong mock opportunity signal.",
            DailyDigestCandidate: true,
            OpportunityDigestCandidate: isOpportunity,
            UrgentCandidate: isUrgent,
            RawResponseJson: "{}");

        return Task.FromResult(result);
    }

    public Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
        NewsArticle article,
        UserPreferences preferences,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new DetailedAnalysisResult(
            $"Mock detailed analysis for: {article.Title}",
            "{}"));
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}