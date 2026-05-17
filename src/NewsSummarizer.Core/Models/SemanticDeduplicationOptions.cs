
namespace NewsSummarizer.Core.Models;

public sealed class SemanticDeduplicationOptions
{
    public bool Enabled { get; set; } = false;
    public int LookbackHours { get; set; } = 48;
    public int RecentCandidateLimit { get; set; } = 500;
    public double DuplicateThreshold { get; set; } = 0.92;
    public int MinTextLength { get; set; } = 20;
}
