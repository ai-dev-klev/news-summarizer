namespace NewsSummarizer.Core.Entities;

public sealed class UserPreferences
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<string> EnabledCategories { get; set; } = [];
    public List<string> UrgentTopics { get; set; } = [];
    public string? ImportantTopicsText { get; set; }
    public string? ExcludedTopicsText { get; set; }
    public bool DailyDigestEnabled { get; set; } = true;
    public TimeOnly? DailyDigestTime { get; set; }
    public bool OpportunityDigestEnabled { get; set; } = true;
    public TimeOnly? OpportunityDigestTime { get; set; }
    public bool UrgentNotificationsEnabled { get; set; } = true;
    public int MaxItemsPerDigest { get; set; } = 10;
    public string Timezone { get; set; } = "Europe/Moscow";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}