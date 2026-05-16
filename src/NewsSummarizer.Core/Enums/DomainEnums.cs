namespace NewsSummarizer.Core.Enums;

public enum UserStatus { Active, Blocked, Disabled }
public enum SourceType { Rss, Api, Manual, Mock }
public enum ArticleStatus { New, Duplicate, PendingAi, Analyzed, Failed, Expired }
public enum AiProviderType { Yandex, Mock, OpenAi, Ollama }
public enum AiResultStatus { Pending, Success, Failed }
public enum DigestType { Daily, Opportunity }
public enum DigestStatus { Created, Sent, Failed }
public enum NotificationType { Urgent, DailyDigest, OpportunityDigest, DetailedAnalysis }
public enum NotificationStatus { Pending, Sent, Failed }