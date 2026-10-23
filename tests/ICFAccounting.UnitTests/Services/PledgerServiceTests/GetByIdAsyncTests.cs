using FakeItEasy;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class GetByIdAsyncTests
{
    [Fact]
    public async Task GetByIdAsync_with_existing_id_returns_pledger()
    {
        var fixture = new PledgerServiceTestFixture();
        var pledger = Pledger.Create("Community Fund");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetByIdAsync(pledger.Id, cancellationToken))
            .Returns(pledger);

        var result = await fixture.Service.GetByIdAsync(pledger.Id, cancellationToken);

        Assert.Same(pledger, result);
        A.CallTo(() => fixture.Repository.GetByIdAsync(pledger.Id, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetByIdAsync_when_pledger_does_not_exist_returns_null()
    {
        var fixture = new PledgerServiceTestFixture();
        var id = PledgerId.New();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetByIdAsync(id, cancellationToken))
            .Returns((Pledger?)null);

        var result = await fixture.Service.GetByIdAsync(id, cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_with_empty_id_throws_without_calling_repository()
    {
        var fixture = new PledgerServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.GetByIdAsync(default));

        A.CallTo(() => fixture.Repository.GetByIdAsync(A<PledgerId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
