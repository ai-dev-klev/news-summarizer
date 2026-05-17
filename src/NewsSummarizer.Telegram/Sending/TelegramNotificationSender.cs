using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Telegram.Api;
using NewsSummarizer.Telegram.Formatting;

namespace NewsSummarizer.Telegram.Sending;

public sealed class TelegramNotificationSender : INotificationSender
{
    private readonly TelegramBotApiClient _botApiClient;
    private readonly TelegramMessageChunker _messageChunker;

    public TelegramNotificationSender(
        TelegramBotApiClient botApiClient,
        TelegramMessageChunker messageChunker)
    {
        _botApiClient = botApiClient;
        _messageChunker = messageChunker;
    }

    public async Task<SendNotificationResult> SendAsync(
        User user,
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        if (user.TelegramUserId <= 0)
        {
            return new SendNotificationResult(false, "User has invalid Telegram id.");
        }

        var text = BuildText(message);

        foreach (var chunk in _messageChunker.Split(text))
        {
            var result = await _botApiClient.SendTextAsync(
                user.TelegramUserId,
                chunk,
                cancellationToken);

            if (!result.Success)
            {
                return new SendNotificationResult(false, result.ErrorMessage);
            }
        }

        return new SendNotificationResult(true);
    }

    private static string BuildText(NotificationMessage message)
    {
        var title = string.IsNullOrWhiteSpace(message.Title)
            ? "Р Р€Р Р†Р ВµР Т‘Р С•Р СР В»Р ВµР Р…Р С‘Р Вµ"
            : message.Title.Trim();

        var body = string.IsNullOrWhiteSpace(message.Body)
            ? string.Empty
            : message.Body.Trim();

        return string.IsNullOrWhiteSpace(body)
            ? title
            : title + "\n\n" + body;
    }
}