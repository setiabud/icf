using FakeItEasy;
using ICFAccounting.Domain.PledgePayments;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class RemoveAsyncTests
{
    [Fact]
    public async Task RemoveAsync_forwards_id_and_cancellation_token()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var id = PledgePaymentId.New();
        using var cancellationTokenSource = new CancellationTokenSource();

        await fixture.Service.RemoveAsync(id, cancellationTokenSource.Token);

        A.CallTo(() => fixture.Repository.RemoveAsync(id, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RemoveAsync_when_repository_reports_missing_payment_propagates_key_not_found()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var id = PledgePaymentId.New();
        var exception = new KeyNotFoundException("Pledge payment was not found.");
        A.CallTo(() => fixture.Repository.RemoveAsync(id, A<CancellationToken>._))
            .ThrowsAsync(exception);

        var result = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => fixture.Service.RemoveAsync(id));

        Assert.Same(exception, result);
    }
}