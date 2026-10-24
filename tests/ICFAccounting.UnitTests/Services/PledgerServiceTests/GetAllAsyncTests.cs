using FakeItEasy;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Services.PledgerServiceTests;

public sealed class GetAllAsyncTests
{
    [Fact]
    public async Task GetAllAsync_returns_pledgers_from_repository()
    {
        var fixture = new PledgerServiceTestFixture();
        IReadOnlyList<Pledger> pledgers =
        [
            Pledger.Create("Community Fund"),
            Pledger.Create("Education Fund")
        ];
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var repositoryResult = PagedPledgersResult.Create(pledgers, 1, 10, 12);
        var defaultQueryCall = A.CallTo(() => fixture.Repository.GetAllAsync(
                A<PledgerQuery>.That.Matches(query => query.Page == 1 && query.PageSize == 20
                    && query.SortBy == PledgerSortBy.PledgerName && query.SortOrder == SortOrder.Ascending), cancellationToken));
        defaultQueryCall.Returns(repositoryResult);

        var result = await fixture.Service.GetAllAsync(cancellationToken: cancellationToken);

        Assert.Same(repositoryResult, result);
        Assert.Equal(pledgers, result.Items);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        defaultQueryCall.MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public async Task GetAllAsync_forwards_pagination_and_sorting_to_repository(SortOrder sortOrder)
    {
        var fixture = new PledgerServiceTestFixture();
        var query = new PledgerQuery(page: 2, pageSize: 5, sortOrder: sortOrder);
        using var tokenSource = new CancellationTokenSource();
        IReadOnlyList<Pledger> pledgers = [Pledger.Create("Education Fund")];
        var repositoryResult = PagedPledgersResult.Create(pledgers, 2, 5, 6);
        A.CallTo(() => fixture.Repository.GetAllAsync(query, tokenSource.Token)).Returns(repositoryResult);

        var result = await fixture.Service.GetAllAsync(query, tokenSource.Token);

        Assert.Same(repositoryResult, result);
        Assert.Equal(pledgers, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        A.CallTo(() => fixture.Repository.GetAllAsync(query, tokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(12, 2)]
    public async Task GetAllAsync_with_empty_page_preserves_total_count(int totalCount, int totalPages)
    {
        var fixture = new PledgerServiceTestFixture();
        var query = new PledgerQuery(page: 3);
        A.CallTo(() => fixture.Repository.GetAllAsync(query, CancellationToken.None))
            .Returns(PagedPledgersResult.Create([], 3, 10, totalCount));

        var result = await fixture.Service.GetAllAsync(query, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(totalPages, result.TotalPages);
    }
}
