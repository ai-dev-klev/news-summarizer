using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Telegram.Sending;

public sealed class TelegramNotificationSender : INotificationSender
{
    public Task<SendNotificationResult> SendAsync(
        User user,
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new SendNotificationResult(true));
    }
}