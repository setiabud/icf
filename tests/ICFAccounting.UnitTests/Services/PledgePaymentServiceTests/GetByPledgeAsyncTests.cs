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
}