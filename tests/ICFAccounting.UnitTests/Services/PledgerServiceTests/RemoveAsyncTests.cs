using FakeItEasy;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class RemoveAsyncTests
{
    [Fact]
    public async Task RemoveAsync_with_id_removes_pledger_from_repository()
    {
        var fixture = new PledgerServiceTestFixture();
        var id = PledgerId.New();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await fixture.Service.RemoveAsync(id, cancellationToken);

        A.CallTo(() => fixture.Repository.RemoveAsync(id, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task RemoveAsync_with_empty_id_throws_without_calling_repository()
    {
        var fixture = new PledgerServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.RemoveAsync(default));

        A.CallTo(() => fixture.Repository.RemoveAsync(A<PledgerId>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
