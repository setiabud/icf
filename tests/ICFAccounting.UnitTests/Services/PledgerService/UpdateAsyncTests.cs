using FakeItEasy;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Services.PledgerService;

public sealed class UpdateAsyncTests
{
    [Fact]
    public async Task UpdateAsync_with_pledger_updates_it_in_repository()
    {
        var fixture = new PledgerServiceTestFixture();
        var pledger = Pledger.Create("Community Fund");
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await fixture.Service.UpdateAsync(pledger, cancellationToken);

        A.CallTo(() => fixture.Repository.UpdateAsync(pledger, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_with_null_pledger_throws_without_calling_repository()
    {
        var fixture = new PledgerServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Service.UpdateAsync(null!));

        A.CallTo(() => fixture.Repository.UpdateAsync(A<Pledger>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task UpdateAsync_with_invalid_name_throws_without_calling_repository(string? name)
    {
        var fixture = new PledgerServiceTestFixture();
        var pledger = PledgerServiceTestFixture.CreatePledgerWithName(name);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => fixture.Service.UpdateAsync(pledger));

        A.CallTo(() => fixture.Repository.UpdateAsync(A<Pledger>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }
}
