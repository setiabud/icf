using FakeItEasy;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Services.PledgeServiceTests;

public sealed class GetByCategoryAsyncTests
{
    [Fact]
    public async Task GetByCategoryAsync_forwards_query_and_cancellation_token()
    {
        var fixture = new PledgeServiceTestFixture();
        var categoryId = PledgeCategoryId.New();
        var query = new PledgeQuery(page: 2, pageSize: 5, sortOrder: SortOrder.Ascending);
        var repositoryResult = PagedPledgesResult.Create([], 2, 5, 0);
        using var cancellationTokenSource = new CancellationTokenSource();
        A.CallTo(() => fixture.Repository.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token);

        Assert.Same(repositoryResult, result);
    }

    [Fact]
    public async Task GetByCategoryAsync_forwards_pagination_options()
    {
        var fixture = new PledgeServiceTestFixture();
        var categoryId = PledgeCategoryId.New();
        var query = new PledgeQuery(page: 4, pageSize: 6);
        var repositoryResult = PagedPledgesResult.Create([], 4, 6, 0);
        using var cancellationTokenSource = new CancellationTokenSource();
        A.CallTo(() => fixture.Repository.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => fixture.Repository.GetByCategoryAsync(categoryId, query, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(PledgeSortBy.Year, SortOrder.Ascending)]
    [InlineData(PledgeSortBy.Year, SortOrder.Descending)]
    [InlineData(PledgeSortBy.Amount, SortOrder.Ascending)]
    [InlineData(PledgeSortBy.Amount, SortOrder.Descending)]
    public async Task GetByCategoryAsync_forwards_sorting_options(PledgeSortBy sortBy, SortOrder sortOrder)
    {
        var fixture = new PledgeServiceTestFixture();
        var categoryId = PledgeCategoryId.New();
        var query = new PledgeQuery(sortBy: sortBy, sortOrder: sortOrder);
        var repositoryResult = PagedPledgesResult.Create([], 1, 20, 0);
        A.CallTo(() => fixture.Repository.GetByCategoryAsync(categoryId, query, CancellationToken.None))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByCategoryAsync(categoryId, query);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => fixture.Repository.GetByCategoryAsync(categoryId, query, CancellationToken.None))
            .MustHaveHappenedOnceExactly();
    }
}