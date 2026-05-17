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
        if (update.CallbackQuery is not null)
        {
            await HandleCallbackAsync(update, cancellationToken);
            return;
        }

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

        await SendResponseAsync(chatId, response, cancellationToken);
    }

    private async Task HandleCallbackAsync(
        TelegramUpdate update,
        CancellationToken cancellationToken)
    {
        var callback = update.CallbackQuery!;

        var chatId = callback.Message?.Chat.Id ?? callback.From.Id;
        var messageId = callback.Message?.MessageId;

        var userSnapshot = new TelegramUserSnapshot(
            callback.From.Id,
            callback.From.Username,
            callback.From.FirstName);

        _logger.LogInformation(
            "Handling Telegram callback. UpdateId: {UpdateId}, ChatId: {ChatId}, Data: {CallbackData}",
            update.UpdateId,
            chatId,
            callback.Data);

        var response = await _commandService.HandleCallbackAsync(
            callback.Data,
            userSnapshot,
            cancellationToken);

        await _botApiClient.AnswerCallbackQueryAsync(
            callback.Id,
            response.CallbackAnswerText,
            cancellationToken);

        if (messageId is not null)
        {
            var editResult = await _botApiClient.EditMessageTextAsync(
                chatId,
                messageId.Value,
                response.Text,
                response.ReplyMarkup,
                cancellationToken);

            if (editResult.Success)
            {
                return;
            }

            _logger.LogWarning(
                "Failed to edit Telegram settings message. ChatId: {ChatId}, Error: {Error}",
                chatId,
                editResult.ErrorMessage);
        }

        await SendResponseAsync(chatId, response, cancellationToken);
    }

    private async Task SendResponseAsync(
        long chatId,
        TelegramCommandResult response,
        CancellationToken cancellationToken)
    {
        if (response.ReplyMarkup is not null)
        {
            var sendResult = await _botApiClient.SendTextAsync(
                chatId,
                response.Text,
                response.ReplyMarkup,
                cancellationToken);

            if (!sendResult.Success)
            {
                _logger.LogWarning(
                    "Failed to send Telegram command response. ChatId: {ChatId}, Error: {Error}",
                    chatId,
                    sendResult.ErrorMessage);
            }

            return;
        }

        foreach (var chunk in _messageChunker.Split(response.Text))
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
