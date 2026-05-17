using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Core.Models;

public sealed record TelegramUserSnapshot(
    long TelegramUserId,
    string? Username,
    string? FirstName);

public sealed record LatestDigestResult(
    User User,
    Digest? Digest,
    IReadOnlyList<DigestItem> Items);