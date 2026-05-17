using Microsoft.Extensions.Logging;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Telegram.Api;
using NewsSummarizer.Telegram.Commands;
using NewsSummarizer.Telegram.Formatting;

namespace NewsSummarizer.Telegram.Bot;

public sealed class TelegramUpdateHandler
{
    private readonly TelegramBotApiClient _botApiClient;
    private readonly BotCommandParser _parser;
    private readonly TelegramCommandService _commandService;
    private readonly TelegramMessageChunker _messageChunker;
    private readonly ILogger<TelegramUpdateHandler> _logger;

    public TelegramUpdateHandler(
        TelegramBotApiClient botApiClient,
        BotCommandParser parser,
        TelegramCommandService commandService,
        TelegramMessageChunker messageChunker,
        ILogger<TelegramUpdateHandler> logger)
    {
        _botApiClient = botApiClient;
        _parser = parser;
        _commandService = commandService;
        _messageChunker = messageChunker;
        _logger = logger;
    }

    public async Task HandleAsync(
        TelegramUpdate update,
        CancellationToken cancellationToken)
    {
        var message = update.Message;

        if (message?.Text is null)
        {
            return;
        }

        var chatId = message.Chat.Id;
        var from = message.From;

        var userSnapshot = new TelegramUserSnapshot(
            from?.Id ?? chatId,
            from?.Username,
            from?.FirstName);

        var command = _parser.Parse(message.Text);

        _logger.LogInformation(
            "Handling Telegram command. UpdateId: {UpdateId}, ChatId: {ChatId}, Command: {Command}",
            update.UpdateId,
            chatId,
            command.Type);

        var response = await _commandService.HandleAsync(
            command,
            userSnapshot,
            cancellationToken);

        foreach (var chunk in _messageChunker.Split(response))
        {
            var sendResult = await _botApiClient.SendTextAsync(
                chatId,
                chunk,
                cancellationToken);

            if (!sendResult.Success)
            {
                _logger.LogWarning(
                    "Failed to send Telegram command response. ChatId: {ChatId}, Error: {Error}",
                    chatId,
                    sendResult.ErrorMessage);
            }
        }
    }
}