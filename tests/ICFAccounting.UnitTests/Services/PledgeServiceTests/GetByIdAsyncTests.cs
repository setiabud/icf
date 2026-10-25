using FakeItEasy;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

public sealed class GetByIdAsyncTests
{
    [Fact]
    public async Task GetByIdAsync_returns_pledge_from_repository()
    {
        var fixture = new PledgeServiceTestFixture();
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New());
        A.CallTo(() => fixture.Repository.GetByIdAsync(pledge.Id, A<CancellationToken>._)).Returns(pledge);

        var result = await fixture.Service.GetByIdAsync(pledge.Id);

        Assert.Same(pledge, result);
    }
}