using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.UseCases;

public sealed class SendPendingNotificationsUseCase
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationSender _notificationSender;

    public SendPendingNotificationsUseCase(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        INotificationSender notificationSender)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _notificationSender = notificationSender;
    }

    public async Task<SendPendingNotificationsSummary> ExecuteAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be greater than zero.");
        }

        var notifications = await _notificationRepository.GetPendingAsync(limit, cancellationToken);

        var sent = 0;
        var failed = 0;
        var skippedNoUser = 0;

        foreach (var notification in notifications)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);

            if (user is null)
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = "User was not found.";
                skippedNoUser++;
                failed++;
                continue;
            }

            var message = new NotificationMessage(
                notification.TitleSnapshot ?? BuildDefaultTitle(notification.NotificationType),
                notification.MessageSnapshot ?? string.Empty);

            try
            {
                var result = await _notificationSender.SendAsync(
                    user,
                    message,
                    cancellationToken);

                if (result.Success)
                {
                    notification.Status = NotificationStatus.Sent;
                    notification.SentAt = DateTimeOffset.UtcNow;
                    notification.ErrorMessage = null;
                    sent++;
                }
                else
                {
                    notification.Status = NotificationStatus.Failed;
                    notification.ErrorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "Notification sender returned failure."
                        : result.ErrorMessage;
                    failed++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = exception.Message;
                failed++;
            }
        }

        await _notificationRepository.SaveChangesAsync(cancellationToken);

        return new SendPendingNotificationsSummary(
            notifications.Count,
            sent,
            failed,
            skippedNoUser);
    }

    private static string BuildDefaultTitle(NotificationType type)
    {
        return type switch
        {
            NotificationType.Urgent => "Срочное уведомление",
            NotificationType.DailyDigest => "Ежедневная сводка",
            NotificationType.OpportunityDigest => "Сводка возможностей",
            NotificationType.DetailedAnalysis => "Подробный анализ",
            _ => "Уведомление"
        };
    }
}
