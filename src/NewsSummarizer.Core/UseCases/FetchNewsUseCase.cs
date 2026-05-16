using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.Services;

namespace NewsSummarizer.Core.UseCases;

public sealed class FetchNewsUseCase
{
    private readonly INewsSourceRepository _sourceRepository;
    private readonly INewsFetcher _newsFetcher;
    private readonly IArticleRepository _articleRepository;
    private readonly ArticleNormalizationService _normalizationService;
    private readonly RetentionPolicyService _retentionPolicyService;

    public FetchNewsUseCase(
        INewsSourceRepository sourceRepository,
        INewsFetcher newsFetcher,
        IArticleRepository articleRepository,
        ArticleNormalizationService normalizationService,
        RetentionPolicyService retentionPolicyService)
    {
        _sourceRepository = sourceRepository;
        _newsFetcher = newsFetcher;
        _articleRepository = articleRepository;
        _normalizationService = normalizationService;
        _retentionPolicyService = retentionPolicyService;
    }

    public async Task<FetchNewsSummary> ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sources = await _sourceRepository.GetEnabledAsync(cancellationToken);

        var sourcesChecked = 0;
        var articlesFetched = 0;
        var articlesAdded = 0;
        var duplicateArticles = 0;
        var skippedArticles = 0;
        var failedSources = 0;

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sourcesChecked++;

            IReadOnlyList<FetchedArticle> fetchedArticles;

            try
            {
                fetchedArticles = await _newsFetcher.FetchAsync(source, cancellationToken);
            }
            catch
            {
                failedSources++;
                continue;
            }

            articlesFetched += fetchedArticles.Count;

            foreach (var fetchedArticle in fetchedArticles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(fetchedArticle.Title) ||
                    string.IsNullOrWhiteSpace(fetchedArticle.Url))
                {
                    skippedArticles++;
                    continue;
                }

                var article = CreateArticle(source, fetchedArticle, now);
                var deduplicationKey = _normalizationService.BuildKey(article);

                var duplicate = await _articleRepository.FindDuplicateAsync(deduplicationKey, cancellationToken);

                if (duplicate is not null)
                {
                    duplicateArticles++;
                    continue;
                }

                await _articleRepository.AddAsync(article, cancellationToken);
                articlesAdded++;
            }
        }

        await _articleRepository.SaveChangesAsync(cancellationToken);

        return new FetchNewsSummary(
            sourcesChecked,
            articlesFetched,
            articlesAdded,
            duplicateArticles,
            skippedArticles,
            failedSources);
    }

    private NewsArticle CreateArticle(NewsSource source, FetchedArticle fetchedArticle, DateTimeOffset now)
    {
        var title = fetchedArticle.Title.Trim();
        var url = fetchedArticle.Url.Trim();
        var normalizedTitle = _normalizationService.NormalizeTitle(title);
        var canonicalUrl = _normalizationService.BuildCanonicalUrl(url);
        var contentForHash = fetchedArticle.Content ?? fetchedArticle.Description;
        var contentHash = _normalizationService.ComputeContentHash(contentForHash);
        var dedupKey = BuildDedupKey(source.Id, canonicalUrl, normalizedTitle, contentHash);

        return new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = source.Id,
            Title = title,
            Url = url,
            CanonicalUrl = canonicalUrl,
            Description = NormalizeOptionalText(fetchedArticle.Description),
            Content = NormalizeOptionalText(fetchedArticle.Content),
            Language = NormalizeOptionalText(fetchedArticle.Language) ?? source.Language,
            PublishedAt = fetchedArticle.PublishedAt,
            FetchedAt = now,
            NormalizedTitle = normalizedTitle,
            ContentHash = contentHash,
            DedupKey = dedupKey,
            Status = ArticleStatus.PendingAi,
            ExpiresAt = _retentionPolicyService.GetArticleExpiresAt(now),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildDedupKey(
        Guid sourceId,
        string? canonicalUrl,
        string normalizedTitle,
        string? contentHash)
    {
        var raw = canonicalUrl ?? contentHash ?? normalizedTitle;
        var key = $"article:{sourceId:N}:{raw}";

        return key.Length <= 512 ? key : key[..512];
    }
}