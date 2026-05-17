
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Interfaces;

public interface ISemanticArticleDuplicateDetector
{
    Task<SemanticDuplicateCheckResult> CheckAndStoreAsync(
        NewsArticle article,
        CancellationToken cancellationToken = default);
}
