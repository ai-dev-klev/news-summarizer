using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Telegram.Sending;

namespace NewsSummarizer.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegram(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<INotificationSender, TelegramNotificationSender>();
        return services;
    }
}