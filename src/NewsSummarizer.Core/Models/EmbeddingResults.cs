
using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Models;

public sealed record EmbeddingResult(
    AiProviderType Provider,
    string Model,
    float[] Vector);

public sealed record SemanticDuplicateCheckResult(
    bool IsDuplicate,
    Guid? DuplicateOfArticleId,
    double Similarity,
    string Reason);
