using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsSummarizer.Telegram.Options;

namespace NewsSummarizer.Telegram.Api;

public sealed class TelegramBotApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramBotApiClient> _logger;

    public TelegramBotApiClient(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        ILogger<TelegramBotApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.BotToken);

    public async Task DeleteWebhookAsync(
        bool dropPendingUpdates,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return;
        }

        var payload = new
        {
            drop_pending_updates = dropPendingUpdates
        };

        try
        {
            await PostAsync<JsonElement>("deleteWebhook", payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Telegram deleteWebhook failed. Long polling may still work.");
        }
    }

    public async Task SetMyCommandsAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return;
        }

        var payload = new
        {
            commands = new[]
            {
                new { command = "start", description = "Создать или обновить профиль" },
                new { command = "help", description = "Показать помощь" },
                new { command = "status", description = "Показать статус профиля" },
                new { command = "digest", description = "Показать ежедневную сводку" },
                new { command = "opportunities", description = "Показать сводку возможностей" },
                new { command = "settings", description = "Показать настройки" },
                new { command = "analyze", description = "Подробный AI-анализ: /analyze <articleId>" }
            }
        };

        try
        {
            await PostAsync<JsonElement>("setMyCommands", payload, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Telegram setMyCommands failed.");
        }
    }

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        int offset,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var payload = new
        {
            offset,
            limit = 20,
            timeout = Math.Max(1, timeoutSeconds),
            allowed_updates = new[] { "message", "callback_query" }
        };

        var result = await PostAsync<List<TelegramUpdate>>(
            "getUpdates",
            payload,
            cancellationToken);

        return result ?? [];
    }

    public Task<TelegramSendResult> SendTextAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        return SendTextAsync(
            chatId,
            text,
            replyMarkup: BuildMainReplyKeyboard(),
            cancellationToken);
    }

    public Task<TelegramSendResult> SendTextAsync(
        long chatId,
        string text,
        object? replyMarkup,
        CancellationToken cancellationToken)
    {
        return SendTextInternalAsync(
            chatId,
            text,
            replyMarkup,
            cancellationToken);
    }

    public Task<TelegramSendResult> SendTextWithoutMarkupAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        return SendTextInternalAsync(
            chatId,
            text,
            replyMarkup: null,
            cancellationToken);
    }

    public async Task<TelegramSendResult> EditMessageTextAsync(
        long chatId,
        int messageId,
        string text,
        object? replyMarkup,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return new TelegramSendResult(false, "Telegram bot token is not configured.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new TelegramSendResult(false, "Telegram message text is empty.");
        }

        var payload = new
        {
            chat_id = chatId,
            message_id = messageId,
            text,
            disable_web_page_preview = true,
            reply_markup = replyMarkup
        };

        try
        {
            await PostAsync<JsonElement>(
                "editMessageText",
                payload,
                cancellationToken);

            return new TelegramSendResult(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new TelegramSendResult(false, exception.Message);
        }
    }

    public async Task AnswerCallbackQueryAsync(
        string callbackQueryId,
        string? text,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(callbackQueryId))
        {
            return;
        }

        var payload = new
        {
            callback_query_id = callbackQueryId,
            text
        };

        try
        {
            await PostAsync<JsonElement>(
                "answerCallbackQuery",
                payload,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Telegram answerCallbackQuery failed.");
        }
    }

    private async Task<TelegramSendResult> SendTextInternalAsync(
        long chatId,
        string text,
        object? replyMarkup,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return new TelegramSendResult(false, "Telegram bot token is not configured.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return new TelegramSendResult(false, "Telegram message text is empty.");
        }

        var payload = new
        {
            chat_id = chatId,
            text,
            disable_web_page_preview = true,
            reply_markup = replyMarkup
        };

        try
        {
            await PostAsync<JsonElement>(
                "sendMessage",
                payload,
                cancellationToken);

            return new TelegramSendResult(true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new TelegramSendResult(false, exception.Message);
        }
    }

    private static object BuildMainReplyKeyboard()
    {
        return new
        {
            keyboard = new[]
            {
                new[]
                {
                    new { text = "/digest" },
                    new { text = "/opportunities" }
                },
                new[]
                {
                    new { text = "/settings" },
                    new { text = "/help" }
                }
            },
            resize_keyboard = true,
            is_persistent = true,
            one_time_keyboard = false,
            input_field_placeholder = "Выбери команду или напиши /analyze <articleId>"
        };
    }

    private async Task<T?> PostAsync<T>(
        string method,
        object payload,
        CancellationToken cancellationToken)
    {
        var url = BuildMethodUrl(method);

        using var response = await _httpClient.PostAsJsonAsync(
            url,
            payload,
            JsonOptions,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        TelegramApiEnvelope<T>? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<TelegramApiEnvelope<T>>(body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Telegram API returned non-JSON response for method '{method}'. HTTP {(int)response.StatusCode}.",
                exception);
        }

        if (!response.IsSuccessStatusCode || envelope is null || !envelope.Ok)
        {
            var description = envelope?.Description;

            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(description)
                    ? $"Telegram API method '{method}' failed. HTTP {(int)response.StatusCode}."
                    : $"Telegram API method '{method}' failed. HTTP {(int)response.StatusCode}. {description}");
        }

        return envelope.Result;
    }

    private string BuildMethodUrl(string method)
    {
        return $"https://api.telegram.org/bot{_options.BotToken.Trim()}/{method}";
    }
}
