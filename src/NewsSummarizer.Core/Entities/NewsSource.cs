using NewsSummarizer.Core.Enums;

namespace NewsSummarizer.Core.Entities;

public sealed class NewsSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SourceType SourceType { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Language { get; set; }
    public List<string> DefaultCategories { get; set; } = [];
    public bool IsEnabled { get; set; } = true;
    public bool IsFastSource { get; set; }
    public int FetchIntervalMinutes { get; set; } = 60;
    public int TrustScore { get; set; } = 50;
    public DateTimeOffset? LastFetchedAt { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}