using System.Text.Json;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;

namespace NewsSummarizer.Core.UseCases;

public sealed class AnalyzeArticleInDetailUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly IArticleRepository _articleRepository;
    private readonly IDetailedAnalysisRepository _analysisRepository;
    private readonly IAiProvider _aiProvider;
    private readonly RetentionPolicyService _retentionPolicyService;

    public AnalyzeArticleInDetailUseCase(
        IUserRepository userRepository,
        IUserPreferencesRepository preferencesRepository,
        IArticleRepository articleRepository,
        IDetailedAnalysisRepository analysisRepository,
        IAiProvider aiProvider,
        RetentionPolicyService retentionPolicyService)
    {
        _userRepository = userRepository;
        _preferencesRepository = preferencesRepository;
        _articleRepository = articleRepository;
        _analysisRepository = analysisRepository;
        _aiProvider = aiProvider;
        _retentionPolicyService = retentionPolicyService;
    }

    public async Task<DetailedAnalysis> ExecuteAsync(
        long telegramUserId,
        Guid articleId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken)
                   ?? throw new InvalidOperationException("Telegram user was not found.");

        var article = await _articleRepository.GetByIdAsync(articleId, cancellationToken)
                      ?? throw new InvalidOperationException("Article was not found.");

        var preferences = await _preferencesRepository.GetByUserIdAsync(user.Id, cancellationToken)
                          ?? CreateFallbackPreferences(user.Id);

        var now = DateTimeOffset.UtcNow;
        var providerInfo = GetProviderInfo();

        DetailedAnalysis analysis;

        try
        {
            var result = await _aiProvider.AnalyzeInDetailAsync(
                article,
                preferences,
                cancellationToken);

            analysis = new DetailedAnalysis
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ArticleId = article.Id,
                Provider = providerInfo.Provider,
                Model = providerInfo.Model,
                PromptVersion = providerInfo.PromptVersion,
                AnalysisText = NormalizeText(result.AnalysisText),
                RawResponseJson = NormalizeRawJson(result.RawResponseJson),
                Status = AiResultStatus.Success,
                CreatedAt = now,
                ExpiresAt = _retentionPolicyService.GetDetailedAnalysisExpiresAt(now)
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            analysis = new DetailedAnalysis
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ArticleId = article.Id,
                Provider = providerInfo.Provider,
                Model = providerInfo.Model,
                PromptVersion = providerInfo.PromptVersion,
                AnalysisText = null,
                RawResponseJson = "{}",
                Status = AiResultStatus.Failed,
                ErrorMessage = exception.Message,
                CreatedAt = now,
                ExpiresAt = _retentionPolicyService.GetDetailedAnalysisExpiresAt(now)
            };
        }

        await _analysisRepository.AddAsync(analysis, cancellationToken);
        await _analysisRepository.SaveChangesAsync(cancellationToken);

        return analysis;
    }

    private ProviderInfo GetProviderInfo()
    {
        if (_aiProvider is IAiProviderInfo providerInfo)
        {
            return new ProviderInfo(
                providerInfo.Provider,
                providerInfo.Model,
                providerInfo.PromptVersion);
        }

        return new ProviderInfo(AiProviderType.Mock, "unknown", "v1");
    }

    private static UserPreferences CreateFallbackPreferences(Guid userId)
    {
        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnabledCategories = [],
            UrgentTopics = [],
            DailyDigestEnabled = true,
            OpportunityDigestEnabled = true,
            UrgentNotificationsEnabled = true,
            MaxItemsPerDigest = 10,
            Timezone = "Europe/Moscow",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "AI did not return detailed analysis text."
            : value.Trim();
    }

    private static string NormalizeRawJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "{}";
        }

        try
        {
            using var _ = JsonDocument.Parse(value);
            return value;
        }
        catch (JsonException)
        {
            return JsonSerializer.Serialize(value);
        }
    }

    private sealed record ProviderInfo(
        AiProviderType Provider,
        string Model,
        string PromptVersion);
}