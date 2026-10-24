using FakeItEasy;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class UpdateAsyncTests
{
    [Fact]
    public async Task UpdateAsync_with_existing_id_updates_and_returns_pledger()
    {
        var fixture = new PledgerServiceTestFixture();
        var pledger = Pledger.Create("Community Fund", fixture.Clock);
        var originalCreatedAt = pledger.CreatedAt;
        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        A.CallTo(() => fixture.Repository.GetByIdAsync(pledger.Id, cancellationToken))
            .Returns(pledger);

        var result = await fixture.Service.UpdateAsync(pledger.Id, "Mission Fund", false, cancellationToken);

        Assert.Same(pledger, result);
        Assert.NotNull(result);
        Assert.Equal("Mission Fund", result.PledgerName);
        Assert.False(result.Active);
        Assert.Equal(originalCreatedAt, result.CreatedAt);
        Assert.Equal(PledgerServiceTestFixture.StartTime.AddMinutes(5).UtcDateTime, result.UpdatedAt);
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
    public async Task UpdateAsync_when_repository_update_fails_propagates_exception()
    {
        var fixture = new PledgerServiceTestFixture();
        var pledger = Pledger.Create("Community Fund");
        var exception = new InvalidOperationException("Repository update failed.");
        A.CallTo(() => fixture.Repository.GetByIdAsync(pledger.Id, A<CancellationToken>._))
            .Returns(pledger);
        A.CallTo(() => fixture.Repository.UpdateAsync(pledger, A<CancellationToken>._))
            .ThrowsAsync(exception);

        var result = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.UpdateAsync(pledger.Id, "Mission Fund", true));

        Assert.Same(exception, result);
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
