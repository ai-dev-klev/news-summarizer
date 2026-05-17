using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsSummarizer.Telegram.Api;
using NewsSummarizer.Telegram.Options;

namespace NewsSummarizer.Telegram.Bot;

public sealed class TelegramBotHostedService : BackgroundService
{
    private readonly TelegramBotApiClient _botApiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramBotHostedService> _logger;

    public TelegramBotHostedService(
        TelegramBotApiClient botApiClient,
        IServiceScopeFactory scopeFactory,
        IOptions<TelegramOptions> options,
        ILogger<TelegramBotHostedService> logger)
    {
        _botApiClient = botApiClient;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.PollingEnabled)
        {
            _logger.LogInformation("Telegram polling is disabled.");
            return;
        }

        if (!_botApiClient.IsConfigured)
        {
            _logger.LogWarning("Telegram bot token is not configured. Telegram bot polling is disabled.");
            return;
        }

        _logger.LogInformation("Telegram bot polling is starting.");

        await _botApiClient.DeleteWebhookAsync(
            dropPendingUpdates: false,
            stoppingToken);

        await _botApiClient.SetMyCommandsAsync(stoppingToken);

        var offset = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await _botApiClient.GetUpdatesAsync(
                    offset,
                    _options.PollingTimeoutSeconds,
                    stoppingToken);

                foreach (var update in updates)
                {
                    offset = Math.Max(offset, update.UpdateId + 1);

                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<TelegramUpdateHandler>();

                    await handler.HandleAsync(update, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Telegram polling iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Telegram bot polling stopped.");
    }
}