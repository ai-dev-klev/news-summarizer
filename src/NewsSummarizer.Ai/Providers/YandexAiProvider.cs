using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Ai.Providers;

public sealed class YandexAiProvider : IAiProvider, IAiProviderInfo
{
    private readonly HttpClient _httpClient;

    public YandexAiProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public AiProviderType Provider => AiProviderType.Yandex;
    public string Model => "yandex-ai";
    public string PromptVersion => "v1";

    public Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(
        NewsArticle article,
        CancellationToken cancellationToken)
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