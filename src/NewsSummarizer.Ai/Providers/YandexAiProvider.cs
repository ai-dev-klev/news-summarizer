using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Clients;
using NewsSummarizer.Ai.Models;
using NewsSummarizer.Ai.Parsing;
using NewsSummarizer.Ai.Prompts;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using OpenAI.Chat;
using System.ClientModel;

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

    public AiProviderType Provider => AiProviderType.Yandex;

    public string Model => _options.Model;

    public string PromptVersion => _options.PromptVersion;

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

        using var timeoutCts = CreateTimeoutTokenSource(cancellationToken);
        var effectiveCancellationToken = timeoutCts.Token;

        var client = _clientFactory.Create();
        var messages = BuildClassificationMessages(article);

        try
        {
            var rawText = await CompleteAsync(
                client,
                messages,
                BuildClassificationOptions(useStructuredOutput: true),
                effectiveCancellationToken);

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
        catch (ClientResultException exception) when (IsUnsupportedStructuredOutputError(exception))
        {
            _logger.LogWarning(
                exception,
                "Yandex AI structured output request failed. Retrying without ResponseFormat. ArticleId: {ArticleId}",
                article.Id);

            var rawText = await CompleteAsync(
                client,
                messages,
                BuildClassificationOptions(useStructuredOutput: false),
                effectiveCancellationToken);

            return _parser.ParseArticleAnalysis(rawText);
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

        using var timeoutCts = CreateTimeoutTokenSource(cancellationToken);
        var effectiveCancellationToken = timeoutCts.Token;

        try
        {
            var client = _clientFactory.Create();
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(DetailedAnalysisPrompt.SystemMessage),
                new UserChatMessage(DetailedAnalysisPrompt.Build(article, preferences))
            };

            var rawText = await CompleteAsync(
                client,
                messages,
                BuildTextOptions(),
                effectiveCancellationToken);

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

    private static List<ChatMessage> BuildClassificationMessages(NewsArticle article)
    {
        return
        [
            new SystemChatMessage(NewsClassificationPrompt.SystemMessage),
            new UserChatMessage(NewsClassificationPrompt.Build(article))
        ];
    }

    private ChatCompletionOptions BuildClassificationOptions(bool useStructuredOutput)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = _options.Temperature,
            MaxOutputTokenCount = _options.MaxOutputTokens
        };

        if (useStructuredOutput)
        {
            options.ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "news_article_analysis",
                jsonSchema: BinaryData.FromString(NewsClassificationPrompt.JsonSchema),
                jsonSchemaIsStrict: true);
        }

        return options;
    }

    private ChatCompletionOptions BuildTextOptions()
    {
        return new ChatCompletionOptions
        {
            Temperature = _options.Temperature,
            MaxOutputTokenCount = Math.Max(_options.MaxOutputTokens, 1200)
        };
    }

    private CancellationTokenSource CreateTimeoutTokenSource(CancellationToken cancellationToken)
    {
        var timeoutSeconds = _options.RequestTimeoutSeconds <= 0
            ? 60
            : _options.RequestTimeoutSeconds;

        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        return timeoutCts;
    }

    private static async Task<string> CompleteAsync(
        ChatClient client,
        IReadOnlyList<ChatMessage> messages,
        ChatCompletionOptions options,
        CancellationToken cancellationToken)
    {
        var completion = await client.CompleteChatAsync(
            messages,
            options,
            cancellationToken);

        return ExtractText(completion.Value);
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

    private static bool IsUnsupportedStructuredOutputError(ClientResultException exception)
    {
        // OpenAI-compatible providers may return a generic 400 for unsupported response_format/json_schema.
        // The fallback repeats the same prompt without ResponseFormat, so real request errors will still fail there.
        return exception.Status == 400;
    }
}
