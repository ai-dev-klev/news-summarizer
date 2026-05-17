using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.UseCases;
using NewsSummarizer.Telegram.Formatting;

namespace NewsSummarizer.Telegram.Commands;

public sealed class TelegramCommandService
{
    private readonly EnsureTelegramUserUseCase _ensureTelegramUser;
    private readonly GetLatestDigestUseCase _getLatestDigest;
    private readonly AnalyzeArticleInDetailUseCase _analyzeArticleInDetail;
    private readonly DigestMessageFormatter _digestFormatter;
    private readonly DetailedAnalysisFormatter _detailedAnalysisFormatter;

    public TelegramCommandService(
        EnsureTelegramUserUseCase ensureTelegramUser,
        GetLatestDigestUseCase getLatestDigest,
        AnalyzeArticleInDetailUseCase analyzeArticleInDetail,
        DigestMessageFormatter digestFormatter,
        DetailedAnalysisFormatter detailedAnalysisFormatter)
    {
        _ensureTelegramUser = ensureTelegramUser;
        _getLatestDigest = getLatestDigest;
        _analyzeArticleInDetail = analyzeArticleInDetail;
        _digestFormatter = digestFormatter;
        _detailedAnalysisFormatter = detailedAnalysisFormatter;
    }

    public async Task<string> HandleAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        return command.Type switch
        {
            BotCommandType.Start => await HandleStartAsync(userSnapshot, cancellationToken),
            BotCommandType.Help => BotCommandHelpText.Build(),
            BotCommandType.Status => await HandleStatusAsync(userSnapshot, cancellationToken),
            BotCommandType.Digest => await HandleDigestAsync(userSnapshot, DigestType.Daily, cancellationToken),
            BotCommandType.Opportunities => await HandleDigestAsync(userSnapshot, DigestType.Opportunity, cancellationToken),
            BotCommandType.Analyze => await HandleAnalyzeAsync(command, userSnapshot, cancellationToken),
            _ => BotCommandResponseText.UnknownCommand()
        };
    }

    private async Task<string> HandleStartAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var user = await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        return BotCommandResponseText.Welcome(user);
    }

    private async Task<string> HandleStatusAsync(
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        var user = await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        return BotCommandResponseText.Status(user);
    }

    private async Task<string> HandleDigestAsync(
        TelegramUserSnapshot userSnapshot,
        DigestType digestType,
        CancellationToken cancellationToken)
    {
        await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        var digest = await _getLatestDigest.ExecuteAsync(
            userSnapshot.TelegramUserId,
            digestType,
            cancellationToken);

        return _digestFormatter.Format(digest, digestType);
    }

    private async Task<string> HandleAnalyzeAsync(
        BotCommand command,
        TelegramUserSnapshot userSnapshot,
        CancellationToken cancellationToken)
    {
        await _ensureTelegramUser.ExecuteAsync(userSnapshot, cancellationToken);

        var articleId = command.FirstArgument;

        if (string.IsNullOrWhiteSpace(articleId) ||
            !Guid.TryParse(articleId, out var parsedArticleId))
        {
            return BotCommandResponseText.AnalyzeUsage();
        }

        try
        {
            var analysis = await _analyzeArticleInDetail.ExecuteAsync(
                userSnapshot.TelegramUserId,
                parsedArticleId,
                cancellationToken);

            return _detailedAnalysisFormatter.Format(analysis);
        }
        catch (InvalidOperationException exception)
        {
            return $"Р СњР Вµ РЎС“Р Т‘Р В°Р В»Р С•РЎРѓРЎРЉ Р Р†РЎвЂ№Р С—Р С•Р В»Р Р…Р С‘РЎвЂљРЎРЉ Р С”Р С•Р СР В°Р Р…Р Т‘РЎС“ /analyze.\n\n{exception.Message}";
        }
    }
}