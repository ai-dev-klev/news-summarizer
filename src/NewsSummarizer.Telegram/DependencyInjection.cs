using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Telegram.Api;
using NewsSummarizer.Telegram.Bot;
using NewsSummarizer.Telegram.Commands;
using NewsSummarizer.Telegram.Formatting;
using NewsSummarizer.Telegram.Options;
using NewsSummarizer.Telegram.Sending;

namespace NewsSummarizer.Telegram;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegram(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));

        services.PostConfigure<TelegramOptions>(options =>
        {
            options.BotToken = FirstNotEmpty(
                Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN"),
                configuration["TELEGRAM_BOT_TOKEN"],
                configuration["Telegram:BotToken"],
                options.BotToken,
                string.Empty)!;

            if (options.PollingTimeoutSeconds <= 0)
            {
                options.PollingTimeoutSeconds = 25;
            }

            if (options.SendChunkLength <= 0 || options.SendChunkLength > TelegramMessageLimits.MaxMessageLength)
            {
                options.SendChunkLength = TelegramMessageLimits.SafeMessageLength;
            }
        });

        services.AddHttpClient<TelegramBotApiClient>();

        services.AddSingleton<BotCommandParser>();
        services.AddSingleton<BotCommandRouter>();
        services.AddSingleton<TelegramMessageChunker>();
        services.AddSingleton<DigestMessageFormatter>();
        services.AddSingleton<DetailedAnalysisFormatter>();
        services.AddSingleton<DetailedAnalysisMessageFormatter>();
        services.AddSingleton<UrgentMessageFormatter>();

        services.AddScoped<TelegramCommandService>();
        services.AddScoped<TelegramUpdateHandler>();
        services.AddScoped<INotificationSender, TelegramNotificationSender>();

        services.AddHostedService<TelegramBotHostedService>();

        return services;
    }

    private static string? FirstNotEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}