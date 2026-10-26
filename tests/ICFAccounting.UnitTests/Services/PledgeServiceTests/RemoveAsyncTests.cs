using FakeItEasy;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

public sealed class RemoveAsyncTests
{
    [Fact]
    public async Task RemoveAsync_forwards_id_and_cancellation_token()
    {
        var fixture = new PledgeServiceTestFixture();
        var id = PledgeId.New();
        using var cancellationTokenSource = new CancellationTokenSource();

        await fixture.Service.RemoveAsync(id, cancellationTokenSource.Token);

        A.CallTo(() => fixture.Repository.RemoveAsync(id, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RemoveAsync_when_repository_reports_missing_pledge_propagates_key_not_found()
    {
        var fixture = new PledgeServiceTestFixture();
        var id = PledgeId.New();
        var exception = new KeyNotFoundException("Pledge was not found.");
        A.CallTo(() => fixture.Repository.RemoveAsync(id, A<CancellationToken>._))
            .ThrowsAsync(exception);

        var result = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => fixture.Service.RemoveAsync(id));

        Assert.Same(exception, result);
    }

    [Fact]
    public async Task RemoveAsync_when_repository_cancels_propagates_cancellation()
    {
        var fixture = new PledgeServiceTestFixture();
        var id = PledgeId.New();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.RemoveAsync(id, cancellationToken))
            .ThrowsAsync(new OperationCanceledException(cancellationToken));

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => fixture.Service.RemoveAsync(id, cancellationToken));

        Assert.Equal(cancellationToken, exception.CancellationToken);
    }

    [Fact]
    public async Task RemoveAsync_with_empty_id_throws_without_calling_repository()
    {
        var fixture = new PledgeServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.RemoveAsync(default));

        A.CallTo(() => fixture.Repository.RemoveAsync(A<PledgeId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}