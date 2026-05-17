using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.UseCases;

public sealed class EnsureTelegramUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserPreferencesRepository _preferencesRepository;

    public EnsureTelegramUserUseCase(
        IUserRepository userRepository,
        IUserPreferencesRepository preferencesRepository)
    {
        _userRepository = userRepository;
        _preferencesRepository = preferencesRepository;
    }

    public async Task<User> ExecuteAsync(
        TelegramUserSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot.TelegramUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshot), "Telegram user id must be positive.");
        }

        var now = DateTimeOffset.UtcNow;
        var user = await _userRepository.GetByTelegramUserIdAsync(snapshot.TelegramUserId, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                TelegramUserId = snapshot.TelegramUserId,
                Username = NormalizeOptional(snapshot.Username),
                FirstName = NormalizeOptional(snapshot.FirstName),
                Status = UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.AddAsync(user, cancellationToken);

            var preferences = CreateDefaultPreferences(user.Id, now);
            await _preferencesRepository.AddAsync(preferences, cancellationToken);
            await _preferencesRepository.SaveChangesAsync(cancellationToken);

            return user;
        }

        var changed = false;

        var username = NormalizeOptional(snapshot.Username);
        if (!string.Equals(user.Username, username, StringComparison.Ordinal))
        {
            user.Username = username;
            changed = true;
        }

        var firstName = NormalizeOptional(snapshot.FirstName);
        if (!string.Equals(user.FirstName, firstName, StringComparison.Ordinal))
        {
            user.FirstName = firstName;
            changed = true;
        }

        if (user.Status != UserStatus.Active)
        {
            user.Status = UserStatus.Active;
            changed = true;
        }

        if (changed)
        {
            user.UpdatedAt = now;
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        var existingPreferences = await _preferencesRepository.GetByUserIdAsync(user.Id, cancellationToken);
        if (existingPreferences is null)
        {
            await _preferencesRepository.AddAsync(CreateDefaultPreferences(user.Id, now), cancellationToken);
            await _preferencesRepository.SaveChangesAsync(cancellationToken);
        }

        return user;
    }

    private static UserPreferences CreateDefaultPreferences(Guid userId, DateTimeOffset now)
    {
        return new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EnabledCategories =
            [
                "general",
                "world",
                "business",
                "technology",
                "science",
                "politics",
                "security",
                "education",
                "health",
                "culture",
                "sports",
                "startups"
            ],
            UrgentTopics =
            [
                "market_crash",
                "critical_event",
                "war",
                "crisis",
                "security"
            ],
            ImportantTopicsText = "technology, business, science, startups, market, security",
            ExcludedTopicsText = null,
            DailyDigestEnabled = true,
            DailyDigestTime = new TimeOnly(9, 0),
            OpportunityDigestEnabled = true,
            OpportunityDigestTime = new TimeOnly(18, 0),
            UrgentNotificationsEnabled = true,
            MaxItemsPerDigest = 10,
            Timezone = "Europe/Moscow",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}