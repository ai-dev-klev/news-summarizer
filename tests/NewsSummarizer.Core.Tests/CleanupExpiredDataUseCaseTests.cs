using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;
using NewsSummarizer.Core.UseCases;

namespace NewsSummarizer.Core.Tests;

public sealed class CleanupExpiredDataUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldCallCleanupRepository_AndReturnSummary()
    {
        var expected = new CleanupExpiredDataSummary(
            ExpiredArticlesDeleted: 3,
            ExpiredNotificationsDeleted: 2,
            ExpiredDetailedAnalysesDeleted: 1);

        var repository = new FakeCleanupRepository(expected);
        var useCase = new CleanupExpiredDataUseCase(repository);

        var before = DateTimeOffset.UtcNow;

        var actual = await useCase.ExecuteAsync(CancellationToken.None);

        var after = DateTimeOffset.UtcNow;

        Assert.Equal(expected, actual);
        Assert.True(repository.Called);
        Assert.True(repository.ReceivedNow >= before.AddSeconds(-1));
        Assert.True(repository.ReceivedNow <= after.AddSeconds(1));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateCancellationToken()
    {
        var repository = new FakeCleanupRepository(new CleanupExpiredDataSummary(0, 0, 0));
        var useCase = new CleanupExpiredDataUseCase(repository);

        using var cancellationTokenSource = new CancellationTokenSource();

        await useCase.ExecuteAsync(cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, repository.ReceivedCancellationToken);
    }

    private sealed class FakeCleanupRepository : ICleanupRepository
    {
        private readonly CleanupExpiredDataSummary _summary;

        public FakeCleanupRepository(CleanupExpiredDataSummary summary)
        {
            _summary = summary;
        }

        public bool Called { get; private set; }
        public DateTimeOffset ReceivedNow { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<CleanupExpiredDataSummary> DeleteExpiredAsync(
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            Called = true;
            ReceivedNow = now;
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(_summary);
        }
    }
}