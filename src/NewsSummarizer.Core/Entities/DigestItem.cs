namespace NewsSummarizer.Core.Entities;

public sealed class DigestItem
{
    public Guid Id { get; set; }
    public Guid DigestId { get; set; }
    public Guid? ArticleId { get; set; }
    public int Position { get; set; }
    public string TitleSnapshot { get; set; } = string.Empty;
    public string? UrlSnapshot { get; set; }
    public string? SourceNameSnapshot { get; set; }
    public string? SummarySnapshot { get; set; }
    public string? ReasonSnapshot { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}