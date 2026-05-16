using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Interfaces;

public interface IAiProvider
{
    Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(NewsArticle article, CancellationToken cancellationToken);

    Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
        NewsArticle article,
        UserPreferences preferences,
        CancellationToken cancellationToken);
}