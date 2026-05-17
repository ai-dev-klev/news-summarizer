using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Core.UseCases;

public sealed class GetLatestDigestUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IDigestRepository _digestRepository;

    public GetLatestDigestUseCase(
        IUserRepository userRepository,
        IDigestRepository digestRepository)
    {
        _userRepository = userRepository;
        _digestRepository = digestRepository;
    }

    public async Task<LatestDigestResult?> ExecuteAsync(
        long telegramUserId,
        DigestType digestType,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var digest = await _digestRepository.GetLatestByUserIdAsync(
            user.Id,
            digestType,
            cancellationToken);

        if (digest is null)
        {
            return new LatestDigestResult(user, null, []);
        }

        var items = await _digestRepository.GetItemsAsync(digest.Id, cancellationToken);

        return new LatestDigestResult(user, digest, items);
    }
}