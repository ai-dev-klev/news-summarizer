using System.Text;
using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Telegram.Formatting;

namespace NewsSummarizer.Telegram.Commands;

public sealed class TelegramCommandService
{
    private static readonly Dictionary<string, string> CategoryAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["general"] = "general",
        ["общие"] = "general",
        ["world"] = "world",
        ["мир"] = "world",
        ["business"] = "business",
        ["бизнес"] = "business",
        ["economy"] = "business",
        ["экономика"] = "business",
        ["technology"] = "technology",
        ["tech"] = "technology",
        ["технологии"] = "technology",
        ["ии"] = "technology",
        ["ai"] = "technology",
        ["science"] = "science",
        ["наука"] = "science",
        ["research"] = "science",
        ["исследования"] = "science",
        ["politics"] = "politics",
        ["политика"] = "politics",
        ["security"] = "security",
        ["безопасность"] = "security",
        ["education"] = "education",
        ["образование"] = "education",
        ["health"] = "health",
        ["здоровье"] = "health",
        ["culture"] = "culture",
        ["культура"] = "culture",
        ["sports"] = "sports",
        ["sport"] = "sports",
        ["спорт"] = "sports",
        ["startups"] = "startups",
        ["startup"] = "startups",
        ["стартапы"] = "startups"
    };

    private static readonly Dictionary<string, string> UrgentTopicAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["market"] = "market",
        ["рынок"] = "market",
        ["market_crash"] = "market_crash",
        ["обвал"] = "market_crash",
        ["crisis"] = "crisis",
        ["кризис"] = "crisis",
        ["security"] = "security",
        ["безопасность"] = "security",
        ["war"] = "war",
        ["война"] = "war",
        ["critical_event"] = "critical_event",
        ["critical"] = "critical_event",
        ["срочно"] = "critical_event"
    };

    private readonly EnsureTelegramUserUseCase _ensureTelegramUser;
    private readonly GetLatestDigestUseCase _getLatestDigest;
    private readonly AnalyzeArticleInDetailUseCase _analyzeArticleInDetail;
    private readonly IUserPreferencesRepository _preferencesRepository;
    private readonly DigestMessageFormatter _digestFormatter;
    private readonly DetailedAnalysisFormatter _detailedAnalysisFormatter;

    public TelegramCommandService(
        EnsureTelegramUserUseCase ensureTelegramUser,
        GetLatestDigestUseCase getLatestDigest,
        AnalyzeArticleInDetailUseCase analyzeArticleInDetail,
        IUserPreferencesRepository preferencesRepository,
        DigestMessageFormatter digestFormatter,
        DetailedAnalysisFormatter detailedAnalysisFormatter)
    {
        _ensureTelegramUser = ensureTelegramUser;
        _getLatestDigest = getLatestDigest;
        _analyzeArticleInDetail = analyzeArticleInDetail;
        _preferencesRepository = preferencesRepository;
        _digestFormatter = digestFormatter;
        _detailedAnalysisFormatter = detailedAnalysisFormatter;
    }

    public async Task<string> HandleAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        return command.Type switch
        {
            BotCommandType.Start => await HandleStartAsync(userSnapshot, cancellationToken),
            BotCommandType.Help => BotCommandHelpText.Build(),
            BotCommandType.Status => await HandleStatusAsync(userSnapshot, cancellationToken),
            BotCommandType.Digest => await HandleDigestAsync(userSnapshot, DigestType.Daily, cancellationToken),
            BotCommandType.Opportunities => await HandleDigestAsync(userSnapshot, DigestType.Opportunity, cancellationToken),
            BotCommandType.Analyze => await HandleAnalyzeAsync(command, userSnapshot, cancellationToken),
            BotCommandType.Settings => await HandleSettingsAsync(userSnapshot, cancellationToken),
            BotCommandType.Categories => await HandleCategoriesAsync(command, userSnapshot, cancellationToken),
            BotCommandType.UrgentTopics => await HandleUrgentTopicsAsync(command, userSnapshot, cancellationToken),
            BotCommandType.DailyOn => await HandleToggleAsync(userSnapshot, "daily", true, cancellationToken),
            BotCommandType.DailyOff => await HandleToggleAsync(userSnapshot, "daily", false, cancellationToken),
            BotCommandType.OpportunitiesOn => await HandleToggleAsync(userSnapshot, "opportunities", true, cancellationToken),
            BotCommandType.OpportunitiesOff => await HandleToggleAsync(userSnapshot, "opportunities", false, cancellationToken),
            BotCommandType.UrgentOn => await HandleToggleAsync(userSnapshot, "urgent", true, cancellationToken),
            BotCommandType.UrgentOff => await HandleToggleAsync(userSnapshot, "urgent", false, cancellationToken),
            BotCommandType.MaxItems => await HandleMaxItemsAsync(command, userSnapshot, cancellationToken),
            _ => BotCommandResponseText.UnknownCommand()
        };
    }

    private async Task<string> HandleStartAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var user = await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        return BotCommandResponseText.Welcome(user);
    }

    private async Task<string> HandleStatusAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var user = await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        return BotCommandResponseText.Status(user);
    }

    private async Task<string> HandleDigestAsync(
        TelegramUserSnapshot userSnapshot,
        DigestType digestType,
        CancellationToken cancellationToken)
    {
        await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        var digest = await _getLatestDigest.ExecuteAsync(
            userSnapshot.TelegramUserId,
            digestType,
            cancellationToken);

        return _digestFormatter.Format(digest, digestType);
    }

    private async Task<string> HandleAnalyzeAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        var articleId = command.FirstArgument;

        if (string.IsNullOrWhiteSpace(articleId) ||
            !Guid.TryParse(articleId, out var parsedArticleId))
        {
            return BotCommandResponseText.AnalyzeUsage();
        }

        try
        {
            var analysis = await _analyzeArticleInDetail.ExecuteAsync(
                userSnapshot.TelegramUserId,
                parsedArticleId,
                cancellationToken);

            return _detailedAnalysisFormatter.Format(analysis);
        }
        catch (InvalidOperationException exception)
        {
            return $"Не удалось выполнить команду /analyze.\n\n{exception.Message}";
        }
    }

    private async Task<string> HandleSettingsAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var (user, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);

        return FormatSettings(user, preferences);
    }

    private async Task<string> HandleCategoriesAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var (_, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);
        var categories = NormalizeTokens(command.Arguments, CategoryAliases);

        if (categories.Count == 0)
        {
            return """
                   Укажи категории после команды.

                   Пример:
                   /categories technology business science

                   Можно по-русски:
                   /categories технологии бизнес наука
                   """;
        }

        preferences.EnabledCategories = categories;
        preferences.UpdatedAt = DateTimeOffset.UtcNow;
        await _preferencesRepository.SaveChangesAsync(cancellationToken);

        return "Категории обновлены.\n\n" + FormatSettings(null, preferences);
    }

    private async Task<string> HandleUrgentTopicsAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var (_, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);
        var topics = NormalizeTokens(command.Arguments, UrgentTopicAliases);

        if (topics.Count == 0)
        {
            return """
                   Укажи темы после команды.

                   Пример:
                   /urgent_topics crisis security market

                   Можно по-русски:
                   /urgent_topics кризис безопасность рынок
                   """;
        }

        preferences.UrgentTopics = topics;
        preferences.UpdatedAt = DateTimeOffset.UtcNow;
        await _preferencesRepository.SaveChangesAsync(cancellationToken);

        return "Темы срочных уведомлений обновлены.\n\n" + FormatSettings(null, preferences);
    }

    private async Task<string> HandleToggleAsync(
        TelegramUserSnapshot userSnapshot,
        string setting,
        bool enabled,
        CancellationToken cancellationToken)
    {
        var (_, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);

        switch (setting)
        {
            case "daily":
                preferences.DailyDigestEnabled = enabled;
                break;
            case "opportunities":
                preferences.OpportunityDigestEnabled = enabled;
                break;
            case "urgent":
                preferences.UrgentNotificationsEnabled = enabled;
                break;
        }

        preferences.UpdatedAt = DateTimeOffset.UtcNow;
        await _preferencesRepository.SaveChangesAsync(cancellationToken);

        return "Настройки обновлены.\n\n" + FormatSettings(null, preferences);
    }

    private async Task<string> HandleMaxItemsAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var (_, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);

        if (command.FirstArgument is null ||
            !int.TryParse(command.FirstArgument, out var maxItems) ||
            maxItems < 1 ||
            maxItems > 20)
        {
            return """
                   Укажи число от 1 до 20.

                   Пример:
                   /max_items 5
                   """;
        }

        preferences.MaxItemsPerDigest = maxItems;
        preferences.UpdatedAt = DateTimeOffset.UtcNow;
        await _preferencesRepository.SaveChangesAsync(cancellationToken);

        return "Размер сводки обновлён.\n\n" + FormatSettings(null, preferences);
    }

    private async Task<(User User, UserPreferences Preferences)> GetUserAndPreferencesAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var user = await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        var preferences = await _preferencesRepository.GetByUserIdAsync(user.Id, cancellationToken);

        if (preferences is null)
        {
            throw new InvalidOperationException("Настройки пользователя не найдены.");
        }

        return (user, preferences);
    }

    private static string FormatSettings(User? user, UserPreferences preferences)
    {
        var builder = new StringBuilder();

        builder.AppendLine("Текущие настройки");
        builder.AppendLine();

        if (user is not null)
        {
            builder.AppendLine($"Пользователь: {user.FirstName ?? user.Username ?? user.TelegramUserId.ToString()}");
            builder.AppendLine();
        }

        builder.AppendLine($"Ежедневная сводка: {FormatEnabled(preferences.DailyDigestEnabled)}");
        builder.AppendLine($"Сводка возможностей: {FormatEnabled(preferences.OpportunityDigestEnabled)}");
        builder.AppendLine($"Срочные уведомления: {FormatEnabled(preferences.UrgentNotificationsEnabled)}");
        builder.AppendLine($"Максимум новостей в сводке: {preferences.MaxItemsPerDigest}");
        builder.AppendLine($"Часовой пояс: {preferences.Timezone}");
        builder.AppendLine();

        builder.AppendLine("Категории:");
        builder.AppendLine(FormatList(preferences.EnabledCategories));
        builder.AppendLine();

        builder.AppendLine("Темы срочных уведомлений:");
        builder.AppendLine(FormatList(preferences.UrgentTopics));
        builder.AppendLine();

        builder.AppendLine("Изменить:");
        builder.AppendLine("/categories technology business science");
        builder.AppendLine("/urgent_topics crisis security market");
        builder.AppendLine("/max_items 5");

        return builder.ToString().Trim();
    }

    private static string FormatEnabled(bool enabled)
    {
        return enabled ? "включено" : "выключено";
    }

    private static string FormatList(IReadOnlyCollection<string> values)
    {
        return values.Count == 0
            ? "[не задано]"
            : string.Join(", ", values);
    }

    private static List<string> NormalizeTokens(
        IReadOnlyList<string> rawArguments,
        IReadOnlyDictionary<string, string> aliases)
    {
        return rawArguments
            .SelectMany(argument => argument.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(argument => argument.Trim().ToLowerInvariant())
            .Where(argument => !string.IsNullOrWhiteSpace(argument))
            .Select(argument => aliases.TryGetValue(argument, out var mapped) ? mapped : argument)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
