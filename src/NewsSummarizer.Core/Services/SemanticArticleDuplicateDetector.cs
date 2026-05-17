
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Services;

public sealed class SemanticArticleDuplicateDetector : ISemanticArticleDuplicateDetector
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IArticleEmbeddingRepository _embeddingRepository;
    private readonly SemanticDeduplicationOptions _options;

    public SemanticArticleDuplicateDetector(
        IEmbeddingProvider embeddingProvider,
        IArticleEmbeddingRepository embeddingRepository,
        SemanticDeduplicationOptions options)
    {
        _embeddingProvider = embeddingProvider;
        _embeddingRepository = embeddingRepository;
        _options = options;
    }

    public async Task<SemanticDuplicateCheckResult> CheckAndStoreAsync(
        NewsArticle article,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);

        if (!_options.Enabled)
        {
            return new SemanticDuplicateCheckResult(false, null, 0, "semantic deduplication disabled");
        }

        if (!_embeddingProvider.IsEnabled)
        {
            return new SemanticDuplicateCheckResult(false, null, 0, "embedding provider disabled or not configured");
        }

        var text = ArticleEmbeddingTextBuilder.Build(article);

        if (text.Length < Math.Max(1, _options.MinTextLength))
        {
            return new SemanticDuplicateCheckResult(false, null, 0, "article text is too short for embeddings");
        }

        var textHash = ArticleEmbeddingTextBuilder.ComputeTextHash(text);
        var currentEmbedding = await _embeddingProvider.CreateEmbeddingAsync(text, cancellationToken);
        var currentVector = currentEmbedding.Vector;

        var since = DateTimeOffset.UtcNow.AddHours(-Math.Max(1, _options.LookbackHours));
        var recentEmbeddings = await _embeddingRepository.GetRecentAsync(
            since,
            Math.Max(1, _options.RecentCandidateLimit),
            cancellationToken);

        ArticleEmbedding? bestMatch = null;
        var bestSimilarity = 0.0;

        foreach (var existing in recentEmbeddings)
        {
            if (existing.ArticleId == article.Id || existing.Embedding.Length != currentVector.Length)
            {
                continue;
            }

            var similarity = VectorMath.CosineSimilarity(currentVector, existing.Embedding);

            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = existing;
            }
        }

        var now = DateTimeOffset.UtcNow;
        await _embeddingRepository.SaveAsync(new ArticleEmbedding
        {
            ArticleId = article.Id,
            Provider = currentEmbedding.Provider,
            Model = currentEmbedding.Model,
            Dimensions = currentVector.Length,
            TextHash = textHash,
            Embedding = currentVector,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        if (bestMatch is null || bestSimilarity < _options.DuplicateThreshold)
        {
            return new SemanticDuplicateCheckResult(false, null, bestSimilarity, "no semantic duplicate found");
        }

        return new SemanticDuplicateCheckResult(
            true,
            bestMatch.ArticleId,
            bestSimilarity,
            "semantic duplicate found by cosine similarity");
    }
}
