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
}