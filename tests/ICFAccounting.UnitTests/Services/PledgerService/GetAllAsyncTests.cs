using FakeItEasy;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Services.PledgerService;

public sealed class GetAllAsyncTests
{
    [Fact]
    public async Task GetAllAsync_returns_pledgers_from_repository()
    {
        var fixture = new PledgerServiceTestFixture();
        IReadOnlyList<Pledger> pledgers =
        [
            Pledger.Create("Community Fund"),
            Pledger.Create("Education Fund")
        ];
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetAllAsync(cancellationToken))
            .Returns(pledgers);

        var result = await fixture.Service.GetAllAsync(cancellationToken);

        Assert.Same(pledgers, result);
        A.CallTo(() => fixture.Repository.GetAllAsync(cancellationToken))
            .MustHaveHappenedOnceExactly();
    }
}
