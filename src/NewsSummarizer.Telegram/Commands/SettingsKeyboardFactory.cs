using NewsSummarizer.Core.Entities;
using NewsSummarizer.Telegram.Api;

namespace NewsSummarizer.Telegram.Commands;

public sealed class SettingsKeyboardFactory
{
    public static readonly IReadOnlyList<SettingsOption> Categories =
    [
        new("general", "Общие"),
        new("world", "Мир"),
        new("business", "Бизнес"),
        new("technology", "Технологии"),
        new("science", "Наука"),
        new("politics", "Политика"),
        new("security", "Безопасность"),
        new("education", "Образование"),
        new("health", "Здоровье"),
        new("culture", "Культура"),
        new("sports", "Спорт"),
        new("startups", "Стартапы")
    ];

    public static readonly IReadOnlyList<SettingsOption> UrgentTopics =
    [
        new("market", "Рынок"),
        new("market_crash", "Обвал рынка"),
        new("crisis", "Кризис"),
        new("security", "Безопасность"),
        new("war", "Война"),
        new("critical_event", "Критические события")
    ];

    public TelegramInlineKeyboardMarkup BuildMain(UserPreferences preferences)
    {
        return Markup(
            Row(Button("Категории", "settings:categories"), Button("Срочные темы", "settings:urgent")),
            Row(Button("Размер сводки", "settings:max")),
            Row(
                Button(preferences.DailyDigestEnabled ? "Ежедневная: вкл" : "Ежедневная: выкл", "settings:toggle:daily"),
                Button(preferences.OpportunityDigestEnabled ? "Возможности: вкл" : "Возможности: выкл", "settings:toggle:opportunities")),
            Row(Button(preferences.UrgentNotificationsEnabled ? "Срочные: вкл" : "Срочные: выкл", "settings:toggle:urgent"))
        );
    }

    public TelegramInlineKeyboardMarkup BuildCategories(UserPreferences preferences)
    {
        var selected = preferences.EnabledCategories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = new List<IReadOnlyList<TelegramInlineKeyboardButton>>();

        foreach (var pair in Categories.Chunk(2))
        {
            rows.Add(pair
                .Select(category => Button(
                    $"{Check(selected.Contains(category.Key))} {category.Title}",
                    $"settings:cat:{category.Key}"))
                .ToArray());
        }

        rows.Add(Row(Button("Назад к настройкам", "settings:main")));

        return new TelegramInlineKeyboardMarkup
        {
            InlineKeyboard = rows
        };
    }

    public TelegramInlineKeyboardMarkup BuildUrgentTopics(UserPreferences preferences)
    {
        var selected = preferences.UrgentTopics.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = new List<IReadOnlyList<TelegramInlineKeyboardButton>>();

        foreach (var pair in UrgentTopics.Chunk(2))
        {
            rows.Add(pair
                .Select(topic => Button(
                    $"{Check(selected.Contains(topic.Key))} {topic.Title}",
                    $"settings:urgent:{topic.Key}"))
                .ToArray());
        }

        rows.Add(Row(Button("Назад к настройкам", "settings:main")));

        return new TelegramInlineKeyboardMarkup
        {
            InlineKeyboard = rows
        };
    }

    public TelegramInlineKeyboardMarkup BuildMaxItems(UserPreferences preferences)
    {
        var values = new[] { 3, 5, 7, 10, 15, 20 };
        var rows = new List<IReadOnlyList<TelegramInlineKeyboardButton>>();

        foreach (var pair in values.Chunk(3))
        {
            rows.Add(pair
                .Select(value => Button(
                    value == preferences.MaxItemsPerDigest ? $"✓ {value}" : value.ToString(),
                    $"settings:max:{value}"))
                .ToArray());
        }

        rows.Add(Row(Button("Назад к настройкам", "settings:main")));

        return new TelegramInlineKeyboardMarkup
        {
            InlineKeyboard = rows
        };
    }

    private static TelegramInlineKeyboardMarkup Markup(params IReadOnlyList<TelegramInlineKeyboardButton>[] rows)
    {
        return new TelegramInlineKeyboardMarkup
        {
            InlineKeyboard = rows
        };
    }

    private static IReadOnlyList<TelegramInlineKeyboardButton> Row(params TelegramInlineKeyboardButton[] buttons)
    {
        return buttons;
    }

    private static TelegramInlineKeyboardButton Button(string text, string callbackData)
    {
        return new TelegramInlineKeyboardButton
        {
            Text = text,
            CallbackData = callbackData
        };
    }

    private static string Check(bool selected)
    {
        return selected ? "✓" : "□";
    }

    public sealed record SettingsOption(string Key, string Title);
}
