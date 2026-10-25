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
}