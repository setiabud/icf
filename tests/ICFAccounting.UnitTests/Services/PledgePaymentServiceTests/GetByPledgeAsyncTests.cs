using FakeItEasy;
using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Services.PledgePaymentServiceTests;

public sealed class GetByPledgeAsyncTests
{
    [Fact]
    public async Task GetByPledgeAsync_uses_default_query()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var pledgeId = PledgeId.New();
        var repositoryResult = PagedPledgePaymentsResult.Create([], 1, 20, 0);
        A.CallTo(() => fixture.Repository.GetByPledgeAsync(
                pledgeId,
                A<PledgePaymentQuery>.That.Matches(query => query.Page == 1 && query.PageSize == 20
                    && query.SortBy == PledgePaymentSortBy.PaymentDate
                    && query.SortOrder == SortOrder.Descending),
                A<CancellationToken>._))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByPledgeAsync(pledgeId);

        Assert.Same(repositoryResult, result);
    }

    [Fact]
    public async Task GetByPledgeAsync_forwards_query_and_cancellation_token()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var pledgeId = PledgeId.New();
        var query = new PledgePaymentQuery(page: 3, pageSize: 7, sortBy: PledgePaymentSortBy.Amount);
        var repositoryResult = PagedPledgePaymentsResult.Create([], 3, 7, 0);
        using var cancellationTokenSource = new CancellationTokenSource();
        A.CallTo(() => fixture.Repository.GetByPledgeAsync(pledgeId, query, cancellationTokenSource.Token))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByPledgeAsync(pledgeId, query, cancellationTokenSource.Token);

        Assert.Same(repositoryResult, result);
        A.CallTo(() => fixture.Repository.GetByPledgeAsync(pledgeId, query, cancellationTokenSource.Token))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Descending)]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Descending)]
    public async Task GetByPledgeAsync_forwards_each_sort_combination(
        PledgePaymentSortBy sortBy, SortOrder sortOrder)
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var pledgeId = PledgeId.New();
        var query = new PledgePaymentQuery(sortBy: sortBy, sortOrder: sortOrder);
        A.CallTo(() => fixture.Repository.GetByPledgeAsync(pledgeId, A<PledgePaymentQuery>._, A<CancellationToken>._))
            .Returns(PagedPledgePaymentsResult.Create([], 1, 20, 0));

        await fixture.Service.GetByPledgeAsync(pledgeId, query);

        A.CallTo(() => fixture.Repository.GetByPledgeAsync(
                pledgeId,
                A<PledgePaymentQuery>.That.Matches(forwarded => forwarded.SortBy == sortBy
                    && forwarded.SortOrder == sortOrder),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetByPledgeAsync_returns_empty_page_metadata_unchanged()
    {
        var fixture = new PledgePaymentServiceTestFixture();
        var pledgeId = PledgeId.New();
        var query = new PledgePaymentQuery(page: 4, pageSize: 5);
        var repositoryResult = PagedPledgePaymentsResult.Create([], 4, 5, 12);
        A.CallTo(() => fixture.Repository.GetByPledgeAsync(pledgeId, query, A<CancellationToken>._))
            .Returns(repositoryResult);

        var result = await fixture.Service.GetByPledgeAsync(pledgeId, query);

        Assert.Empty(result.Items);
        Assert.Equal(4, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }
}