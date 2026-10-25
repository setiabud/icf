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
}