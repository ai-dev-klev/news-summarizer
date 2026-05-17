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
    private readonly SettingsKeyboardFactory _settingsKeyboardFactory;

    public TelegramCommandService(
        EnsureTelegramUserUseCase ensureTelegramUser,
        GetLatestDigestUseCase getLatestDigest,
        AnalyzeArticleInDetailUseCase analyzeArticleInDetail,
        IUserPreferencesRepository preferencesRepository,
        DigestMessageFormatter digestFormatter,
        DetailedAnalysisFormatter detailedAnalysisFormatter,
        SettingsKeyboardFactory settingsKeyboardFactory)
    {
        _ensureTelegramUser = ensureTelegramUser;
        _getLatestDigest = getLatestDigest;
        _analyzeArticleInDetail = analyzeArticleInDetail;
        _preferencesRepository = preferencesRepository;
        _digestFormatter = digestFormatter;
        _detailedAnalysisFormatter = detailedAnalysisFormatter;
        _settingsKeyboardFactory = settingsKeyboardFactory;
    }

    public async Task<TelegramCommandResult> HandleAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        return command.Type switch
        {
            BotCommandType.Start => await HandleStartAsync(userSnapshot, cancellationToken),
            BotCommandType.Help => new TelegramCommandResult(BotCommandHelpText.Build()),
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
            _ => new TelegramCommandResult(BotCommandResponseText.UnknownCommand())
        };
    }

    public async Task<TelegramCommandResult> HandleCallbackAsync(
        string? callbackData,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(callbackData))
        {
            return new TelegramCommandResult(
                "Пустое действие.",
                CallbackAnswerText: "Действие не распознано.");
        }

        var (_, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);

        if (callbackData == "settings:main")
        {
            return new TelegramCommandResult(
                FormatSettings(null, preferences),
                _settingsKeyboardFactory.BuildMain(preferences));
        }

        if (callbackData == "settings:categories")
        {
            return new TelegramCommandResult(
                BuildCategoriesScreenText(preferences),
                _settingsKeyboardFactory.BuildCategories(preferences));
        }

        if (callbackData == "settings:urgent")
        {
            return new TelegramCommandResult(
                BuildUrgentTopicsScreenText(preferences),
                _settingsKeyboardFactory.BuildUrgentTopics(preferences));
        }

        if (callbackData == "settings:max")
        {
            return new TelegramCommandResult(
                BuildMaxItemsScreenText(preferences),
                _settingsKeyboardFactory.BuildMaxItems(preferences));
        }

        if (callbackData.StartsWith("settings:cat:", StringComparison.OrdinalIgnoreCase))
        {
            var category = callbackData["settings:cat:".Length..];
            var message = ToggleListValue(preferences.EnabledCategories, category, allowEmpty: false);

            preferences.UpdatedAt = DateTimeOffset.UtcNow;
            await _preferencesRepository.SaveChangesAsync(cancellationToken);

            return new TelegramCommandResult(
                BuildCategoriesScreenText(preferences),
                _settingsKeyboardFactory.BuildCategories(preferences),
                message);
        }

        if (callbackData.StartsWith("settings:urgent:", StringComparison.OrdinalIgnoreCase))
        {
            var topic = callbackData["settings:urgent:".Length..];
            var message = ToggleListValue(preferences.UrgentTopics, topic, allowEmpty: false);

            preferences.UpdatedAt = DateTimeOffset.UtcNow;
            await _preferencesRepository.SaveChangesAsync(cancellationToken);

            return new TelegramCommandResult(
                BuildUrgentTopicsScreenText(preferences),
                _settingsKeyboardFactory.BuildUrgentTopics(preferences),
                message);
        }

        if (callbackData.StartsWith("settings:max:", StringComparison.OrdinalIgnoreCase))
        {
            var rawMaxItems = callbackData["settings:max:".Length..];

            if (int.TryParse(rawMaxItems, out var maxItems) &&
                maxItems is >= 1 and <= 20)
            {
                preferences.MaxItemsPerDigest = maxItems;
                preferences.UpdatedAt = DateTimeOffset.UtcNow;
                await _preferencesRepository.SaveChangesAsync(cancellationToken);

                return new TelegramCommandResult(
                    BuildMaxItemsScreenText(preferences),
                    _settingsKeyboardFactory.BuildMaxItems(preferences),
                    $"Размер сводки: {maxItems}");
            }
        }

        if (callbackData.StartsWith("settings:toggle:", StringComparison.OrdinalIgnoreCase))
        {
            var toggle = callbackData["settings:toggle:".Length..];

            switch (toggle)
            {
                case "daily":
                    preferences.DailyDigestEnabled = !preferences.DailyDigestEnabled;
                    break;
                case "opportunities":
                    preferences.OpportunityDigestEnabled = !preferences.OpportunityDigestEnabled;
                    break;
                case "urgent":
                    preferences.UrgentNotificationsEnabled = !preferences.UrgentNotificationsEnabled;
                    break;
                default:
                    return new TelegramCommandResult(
                        FormatSettings(null, preferences),
                        _settingsKeyboardFactory.BuildMain(preferences),
                        "Неизвестная настройка.");
            }

            preferences.UpdatedAt = DateTimeOffset.UtcNow;
            await _preferencesRepository.SaveChangesAsync(cancellationToken);

            return new TelegramCommandResult(
                FormatSettings(null, preferences),
                _settingsKeyboardFactory.BuildMain(preferences),
                "Настройка обновлена.");
        }

        return new TelegramCommandResult(
            FormatSettings(null, preferences),
            _settingsKeyboardFactory.BuildMain(preferences),
            "Действие не распознано.");
    }

    private async Task<TelegramCommandResult> HandleStartAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var user = await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        return new TelegramCommandResult(BotCommandResponseText.Welcome(user));
    }

    private async Task<TelegramCommandResult> HandleStatusAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var user = await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        return new TelegramCommandResult(BotCommandResponseText.Status(user));
    }

    private async Task<TelegramCommandResult> HandleDigestAsync(
        TelegramUserSnapshot userSnapshot,
        DigestType digestType,
        CancellationToken cancellationToken)
    {
        await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        var digest = await _getLatestDigest.ExecuteAsync(
            userSnapshot.TelegramUserId,
            digestType,
            cancellationToken);

        return new TelegramCommandResult(_digestFormatter.Format(digest, digestType));
    }

    private async Task<TelegramCommandResult> HandleAnalyzeAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        var articleId = command.FirstArgument;

        if (string.IsNullOrWhiteSpace(articleId) ||
            !Guid.TryParse(articleId, out var parsedArticleId))
        {
            return new TelegramCommandResult(BotCommandResponseText.AnalyzeUsage());
        }

        try
        {
            var analysis = await _analyzeArticleInDetail.ExecuteAsync(
                userSnapshot.TelegramUserId,
                parsedArticleId,
                cancellationToken);

            return new TelegramCommandResult(_detailedAnalysisFormatter.Format(analysis));
        }
        catch (InvalidOperationException exception)
        {
            return new TelegramCommandResult($"Не удалось выполнить команду /analyze.\n\n{exception.Message}");
        }
    }

    private async Task<TelegramCommandResult> HandleSettingsAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var (user, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);

        return new TelegramCommandResult(
            FormatSettings(user, preferences),
            _settingsKeyboardFactory.BuildMain(preferences));
    }

    private async Task<TelegramCommandResult> HandleCategoriesAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var (_, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);
        var categories = NormalizeTokens(command.Arguments, CategoryAliases);

        if (categories.Count == 0)
        {
            return new TelegramCommandResult(
                BuildCategoriesScreenText(preferences),
                _settingsKeyboardFactory.BuildCategories(preferences));
        }

        preferences.EnabledCategories = categories;
        preferences.UpdatedAt = DateTimeOffset.UtcNow;
        await _preferencesRepository.SaveChangesAsync(cancellationToken);

        return new TelegramCommandResult(
            BuildCategoriesScreenText(preferences),
            _settingsKeyboardFactory.BuildCategories(preferences));
    }

    private async Task<TelegramCommandResult> HandleUrgentTopicsAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var (_, preferences) = await GetUserAndPreferencesAsync(userSnapshot, cancellationToken);
        var topics = NormalizeTokens(command.Arguments, UrgentTopicAliases);

        if (topics.Count == 0)
        {
            return new TelegramCommandResult(
                BuildUrgentTopicsScreenText(preferences),
                _settingsKeyboardFactory.BuildUrgentTopics(preferences));
        }

        preferences.UrgentTopics = topics;
        preferences.UpdatedAt = DateTimeOffset.UtcNow;
        await _preferencesRepository.SaveChangesAsync(cancellationToken);

        return new TelegramCommandResult(
            BuildUrgentTopicsScreenText(preferences),
            _settingsKeyboardFactory.BuildUrgentTopics(preferences));
    }

    private async Task<TelegramCommandResult> HandleToggleAsync(
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

        return new TelegramCommandResult(
            FormatSettings(null, preferences),
            _settingsKeyboardFactory.BuildMain(preferences));
    }

    private async Task<TelegramCommandResult> HandleMaxItemsAsync(
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
            return new TelegramCommandResult(
                BuildMaxItemsScreenText(preferences),
                _settingsKeyboardFactory.BuildMaxItems(preferences));
        }

        preferences.MaxItemsPerDigest = maxItems;
        preferences.UpdatedAt = DateTimeOffset.UtcNow;
        await _preferencesRepository.SaveChangesAsync(cancellationToken);

        return new TelegramCommandResult(
            BuildMaxItemsScreenText(preferences),
            _settingsKeyboardFactory.BuildMaxItems(preferences));
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

    private static string BuildCategoriesScreenText(UserPreferences preferences)
    {
        return "Выбери категории новостей.\n\nОтмеченные категории попадут в ежедневную и opportunity-сводку.\n\nСейчас выбрано:\n" +
               FormatList(preferences.EnabledCategories);
    }

    private static string BuildUrgentTopicsScreenText(UserPreferences preferences)
    {
        return "Выбери темы для срочных уведомлений.\n\nЕсли тема отмечена, бот будет чаще пропускать такие новости в urgent-уведомления.\n\nСейчас выбрано:\n" +
               FormatList(preferences.UrgentTopics);
    }

    private static string BuildMaxItemsScreenText(UserPreferences preferences)
    {
        return $"Выбери максимальное количество новостей в одной сводке.\n\nСейчас: {preferences.MaxItemsPerDigest}";
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

        builder.AppendLine("Используй кнопки ниже, чтобы изменить настройки.");

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

    private static string ToggleListValue(
        List<string> values,
        string value,
        bool allowEmpty)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Пустое значение.";
        }

        var normalized = value.Trim().ToLowerInvariant();
        var existing = values.FirstOrDefault(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            if (!allowEmpty && values.Count <= 1)
            {
                return "Нужно оставить хотя бы один пункт.";
            }

            values.Remove(existing);
            return "Отключено.";
        }

        values.Add(normalized);
        values.Sort(StringComparer.OrdinalIgnoreCase);
        return "Включено.";
    }
}
