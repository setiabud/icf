using FakeItEasy;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.PledgeCategories;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

public sealed class AddAsyncTests
{
    [Fact]
    public async Task AddAsync_forwards_pledge_and_cancellation_token()
    {
        var fixture = new PledgeServiceTestFixture();
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New());
        using var cancellationTokenSource = new CancellationTokenSource();

        await fixture.Service.AddAsync(pledge, cancellationTokenSource.Token);

        A.CallTo(() => fixture.Repository.AddAsync(pledge, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task AddAsync_with_null_pledge_throws_without_calling_repository()
    {
        var fixture = new PledgeServiceTestFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.Service.AddAsync(null!));

        A.CallTo(() => fixture.Repository.AddAsync(A<Pledge>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Fact]
    public async Task AddAsync_when_repository_fails_propagates_exception()
    {
        var fixture = new PledgeServiceTestFixture();
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New());
        var exception = new InvalidOperationException("Repository failure.");
        A.CallTo(() => fixture.Repository.AddAsync(pledge, A<CancellationToken>._))
            .ThrowsAsync(exception);

        var result = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.AddAsync(pledge));

        Assert.Same(exception, result);
    }
}