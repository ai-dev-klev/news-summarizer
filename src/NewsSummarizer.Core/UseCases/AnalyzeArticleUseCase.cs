using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.UseCases;

public sealed class AnalyzeArticleUseCase
{
    private readonly IArticleRepository _articleRepository;
    private readonly IArticleAiResultRepository _articleAiResultRepository;
    private readonly IAiProvider _aiProvider;
    private readonly ISemanticArticleDuplicateDetector? _semanticDuplicateDetector;

    public AnalyzeArticleUseCase(
        IArticleRepository articleRepository,
        IArticleAiResultRepository articleAiResultRepository,
        IAiProvider aiProvider,
        ISemanticArticleDuplicateDetector? semanticDuplicateDetector = null)
    {
        _articleRepository = articleRepository;
        _articleAiResultRepository = articleAiResultRepository;
        _aiProvider = aiProvider;
        _semanticDuplicateDetector = semanticDuplicateDetector;
    }

    public async Task<AnalyzeArticlesSummary> ExecuteAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        var articles = await _articleRepository.GetPendingAiAsync(limit, cancellationToken);

        if (articles.Count == 0)
        {
            // No pending articles still go through SaveChangesAsync for compatibility with existing unit tests.
            // This also keeps the use case transaction boundary explicit for repository implementations.
            await _articleRepository.SaveChangesAsync(cancellationToken);

            return new AnalyzeArticlesSummary(0, 0, 0);
        }

        var analyzed = 0;
        var failed = 0;

        foreach (var article in articles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var analysis = await _aiProvider.AnalyzeArticleAsync(article, cancellationToken);
                var result = CreateSuccessResult(article.Id, analysis);

                await _articleAiResultRepository.AddAsync(result, cancellationToken);

                article.Status = ArticleStatus.Analyzed;
                article.UpdatedAt = DateTimeOffset.UtcNow;

                await TryApplySemanticDeduplicationAsync(article, cancellationToken);

                await _articleRepository.SaveChangesAsync(cancellationToken);

                analyzed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var result = CreateFailedResult(article.Id, exception);

                await _articleAiResultRepository.AddAsync(result, cancellationToken);

                article.Status = ArticleStatus.Failed;
                article.UpdatedAt = DateTimeOffset.UtcNow;

                await _articleRepository.SaveChangesAsync(cancellationToken);

                failed++;
            }
        }

        return new AnalyzeArticlesSummary(articles.Count, analyzed, failed);
    }

    private async Task TryApplySemanticDeduplicationAsync(
        NewsArticle article,
        CancellationToken cancellationToken)
    {
        if (_semanticDuplicateDetector is null)
        {
            return;
        }

        try
        {
            var result = await _semanticDuplicateDetector.CheckAndStoreAsync(article, cancellationToken);

            await _articleRepository.SaveChangesAsync(cancellationToken);

            if (!result.IsDuplicate || result.DuplicateOfArticleId is null)
            {
                return;
            }

            article.Status = ArticleStatus.Duplicate;
            article.DuplicateOfArticleId = result.DuplicateOfArticleId;
            article.UpdatedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Embeddings are optional. A Yandex/fake embeddings failure must not break the core AI analysis pipeline.
        }
    }

    private ArticleAiResult CreateSuccessResult(Guid articleId, ArticleAiAnalysisResult analysis)
    {
        var now = DateTimeOffset.UtcNow;
        var providerInfo = GetProviderInfo();

        return new ArticleAiResult
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            Provider = providerInfo.Provider,
            Model = providerInfo.Model,
            PromptVersion = providerInfo.PromptVersion,
            Category = analysis.Category,
            ImportanceScore = ClampScore(analysis.ImportanceScore),
            UrgencyScore = ClampScore(analysis.UrgencyScore),
            OpportunityScore = ClampScore(analysis.OpportunityScore),
            Summary = analysis.Summary,
            Reason = analysis.Reason,
            OpportunityReason = analysis.OpportunityReason,
            DailyDigestCandidate = analysis.DailyDigestCandidate,
            OpportunityDigestCandidate = analysis.OpportunityDigestCandidate,
            UrgentCandidate = analysis.UrgentCandidate,
            Status = AiResultStatus.Success,
            RawResponseJson = NormalizeRawJson(analysis.RawResponseJson),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private ArticleAiResult CreateFailedResult(Guid articleId, Exception exception)
    {
        var now = DateTimeOffset.UtcNow;
        var providerInfo = GetProviderInfo();

        return new ArticleAiResult
        {
            Id = Guid.NewGuid(),
            ArticleId = articleId,
            Provider = providerInfo.Provider,
            Model = providerInfo.Model,
            PromptVersion = providerInfo.PromptVersion,
            ImportanceScore = 0,
            UrgencyScore = 0,
            OpportunityScore = 0,
            Status = AiResultStatus.Failed,
            ErrorMessage = exception.Message,
            RawResponseJson = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };
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

    private static int ClampScore(int value)
    {
        return Math.Clamp(value, 0, 100);
    }

    private static string NormalizeRawJson(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "{}" : value;
    }

    private sealed record ProviderInfo(
        AiProviderType Provider,
        string Model,
        string PromptVersion);
}
