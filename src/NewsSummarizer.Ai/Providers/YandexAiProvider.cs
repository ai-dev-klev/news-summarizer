using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Clients;
using NewsSummarizer.Ai.Models;
using NewsSummarizer.Ai.Parsing;
using NewsSummarizer.Ai.Prompts;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using OpenAI.Chat;

namespace NewsSummarizer.Ai.Providers;

public sealed class YandexAiProvider : IAiProvider
{
    private readonly YandexChatClientFactory _clientFactory;
    private readonly AiResponseParser _parser;
    private readonly AiProviderOptions _options;
    private readonly ILogger<YandexAiProvider> _logger;

    public YandexAiProvider(
        YandexChatClientFactory clientFactory,
        AiResponseParser parser,
        IOptions<AiProviderOptions> options,
        ILogger<YandexAiProvider> logger)
    {
        _clientFactory = clientFactory;
        _parser = parser;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ArticleAiAnalysisResult> AnalyzeArticleAsync(
        NewsArticle article,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(article);

        _logger.LogInformation(
            "Starting Yandex AI article analysis. ArticleId: {ArticleId}, TitleLength: {TitleLength}, ContentLength: {ContentLength}",
            article.Id,
            article.Title.Length,
            article.Content?.Length ?? 0);

        try
        {
            var client = _clientFactory.Create();

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(NewsClassificationPrompt.SystemMessage),
                ChatMessage.CreateUserMessage(NewsClassificationPrompt.Build(article))
            };

            var completion = await client.CompleteChatAsync(
                messages,
                BuildClassificationOptions(),
                cancellationToken);

            var rawText = ExtractText(completion.Value);

            var result = _parser.ParseArticleAnalysis(rawText);

            _logger.LogInformation(
                "Yandex AI article analysis finished. ArticleId: {ArticleId}, Category: {Category}, Importance: {Importance}, Urgency: {Urgency}, Opportunity: {Opportunity}",
                article.Id,
                result.Category,
                result.ImportanceScore,
                result.UrgencyScore,
                result.OpportunityScore);

            return result;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Yandex AI article analysis failed. ArticleId: {ArticleId}",
                article.Id);

            throw;
        }
    }

    public async Task<DetailedAnalysisResult> AnalyzeInDetailAsync(
        NewsArticle article,
        UserPreferences preferences,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(article);
        ArgumentNullException.ThrowIfNull(preferences);

        _logger.LogInformation(
            "Starting Yandex AI detailed analysis. ArticleId: {ArticleId}, UserId: {UserId}",
            article.Id,
            preferences.UserId);

        try
        {
            var client = _clientFactory.Create();

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(DetailedAnalysisPrompt.SystemMessage),
                ChatMessage.CreateUserMessage(DetailedAnalysisPrompt.Build(article, preferences))
            };

            var completion = await client.CompleteChatAsync(
                messages,
                BuildTextOptions(),
                cancellationToken);

            var rawText = ExtractText(completion.Value);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                throw new InvalidOperationException("Yandex AI returned empty detailed analysis response.");
            }

            return new DetailedAnalysisResult(rawText.Trim(), rawText);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Yandex AI detailed analysis failed. ArticleId: {ArticleId}, UserId: {UserId}",
                article.Id,
                preferences.UserId);

            throw;
        }
    }

    private ChatCompletionOptions BuildClassificationOptions()
    {
        return new ChatCompletionOptions
        {
            Temperature = _options.Temperature,
            MaxOutputTokenCount = _options.MaxOutputTokens,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "news_article_analysis",
                jsonSchema: BinaryData.FromString(NewsClassificationPrompt.JsonSchema),
                jsonSchemaIsStrict: true)
        };
    }

    private ChatCompletionOptions BuildTextOptions()
    {
        return new ChatCompletionOptions
        {
            Temperature = _options.Temperature,
            MaxOutputTokenCount = Math.Max(_options.MaxOutputTokens, 1200)
        };
    }

    private static string ExtractText(ChatCompletion completion)
    {
        if (completion.Content.Count == 0)
        {
            throw new InvalidOperationException("Yandex AI returned empty content.");
        }

        var text = completion.Content[0].Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Yandex AI returned empty text content.");
        }

        return text;
    }
}
