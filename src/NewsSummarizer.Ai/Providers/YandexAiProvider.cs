using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Ai.Providers;

public sealed class YandexAiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;

    public YandexAiProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(NewsArticle article, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Yandex AI provider is not implemented yet.");
    }

    public Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
        NewsArticle article,
        UserPreferences preferences,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Yandex AI provider is not implemented yet.");
    }
}