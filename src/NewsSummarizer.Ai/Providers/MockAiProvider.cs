using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Ai.Providers;

public sealed class MockAiProvider : IAiProvider
{
    public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        var result = new ArticleAiAnalysisResult(
            "general",
            50,
            10,
            40,
            "Sample summary",
            "Sample reason",
            "Sample opportunity reason",
            true,
            false,
            false,
            "{}");

        return Task.FromResult(result);
    }

    public Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
        NewsArticle article,
        UserPreferences preferences,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new DetailedAnalysisResult("Sample detailed analysis", "{}"));
    }
}