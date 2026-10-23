using FakeItEasy;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class UpdateAsyncTests
{
    [Fact]
    public async Task UpdateAsync_with_existing_id_updates_and_returns_pledger()
    {
        var fixture = new PledgerServiceTestFixture();
        var pledger = Pledger.Create("Community Fund");
        var originalUpdatedAt = pledger.UpdatedAt;
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetByIdAsync(pledger.Id, cancellationToken))
            .Returns(pledger);

        var result = await fixture.Service.UpdateAsync(pledger.Id, "Mission Fund", false, cancellationToken);

        Assert.Same(pledger, result);
        Assert.Equal("Mission Fund", result.PledgerName);
        Assert.False(result.Active);
        Assert.True(result.UpdatedAt >= originalUpdatedAt);
        A.CallTo(() => fixture.Repository.UpdateAsync(pledger, cancellationToken))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task UpdateAsync_when_pledger_does_not_exist_returns_null_without_updating()
    {
        var fixture = new PledgerServiceTestFixture();
        var id = PledgerId.New();
        A.CallTo(() => fixture.Repository.GetByIdAsync(id, A<CancellationToken>._))
            .Returns((Pledger?)null);

        var result = await fixture.Service.UpdateAsync(id, "Mission Fund", true);

        Assert.Null(result);
        A.CallTo(() => fixture.Repository.UpdateAsync(A<Pledger>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task UpdateAsync_with_empty_id_throws_without_calling_repository()
    {
        var fixture = new PledgerServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Service.UpdateAsync(default, "Mission Fund", true));

        Assert.Empty(Fake.GetCalls(fixture.Repository));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task UpdateAsync_with_invalid_name_throws_without_calling_repository(string? name)
    {
        var fixture = new PledgerServiceTestFixture();

        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => fixture.Service.UpdateAsync(PledgerId.New(), name!, true));

        Assert.Empty(Fake.GetCalls(fixture.Repository));
    }
}
