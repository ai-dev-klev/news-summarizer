using NewsSummarizer.Core.Entities;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.Interfaces;

public interface INotificationSender
{
    Task<SendNotificationResult> SendAsync(User user, NotificationMessage message, CancellationToken cancellationToken);
}