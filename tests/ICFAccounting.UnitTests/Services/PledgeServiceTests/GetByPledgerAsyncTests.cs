using FakeItEasy;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

public sealed class GetByPledgerAsyncTests
{
    [Fact]
    public async Task GetByPledgerAsync_uses_default_query_and_returns_repository_page()
    {
        var fixture = new PledgeServiceTestFixture();
        var pledgerId = PledgerId.New();
        var repositoryResult = PagedPledgesResult.Create([], 1, 20, 0);
        A.CallTo(() => fixture.Repository.GetByPledgerAsync(
                pledgerId,
                A<PledgeQuery>.That.Matches(query => query.Page == 1 && query.PageSize == 20
                    && query.SortBy == PledgeSortBy.Year && query.SortOrder == SortOrder.Descending),
                A<CancellationToken>._))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByPledgerAsync(pledgerId);

        Assert.Same(repositoryResult, result);
    }

    [Fact]
    public async Task GetByPledgerAsync_forwards_pagination_options()
    {
        var fixture = new PledgeServiceTestFixture();
        var pledgerId = PledgerId.New();
        var query = new PledgeQuery(page: 3, pageSize: 7);
        var repositoryResult = PagedPledgesResult.Create([], 3, 7, 0);
        using var cancellationTokenSource = new CancellationTokenSource();
        A.CallTo(() => fixture.Repository.GetByPledgerAsync(pledgerId, query, cancellationTokenSource.Token))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByPledgerAsync(pledgerId, query, cancellationTokenSource.Token);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => fixture.Repository.GetByPledgerAsync(pledgerId, query, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(PledgeSortBy.Year, SortOrder.Ascending)]
    [InlineData(PledgeSortBy.Year, SortOrder.Descending)]
    [InlineData(PledgeSortBy.Amount, SortOrder.Ascending)]
    [InlineData(PledgeSortBy.Amount, SortOrder.Descending)]
    public async Task GetByPledgerAsync_forwards_sorting_options(PledgeSortBy sortBy, SortOrder sortOrder)
    {
        var fixture = new PledgeServiceTestFixture();
        var pledgerId = PledgerId.New();
        var query = new PledgeQuery(sortBy: sortBy, sortOrder: sortOrder);
        var repositoryResult = PagedPledgesResult.Create([], 1, 20, 0);
        A.CallTo(() => fixture.Repository.GetByPledgerAsync(pledgerId, query, CancellationToken.None))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByPledgerAsync(pledgerId, query);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => fixture.Repository.GetByPledgerAsync(pledgerId, query, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }
}