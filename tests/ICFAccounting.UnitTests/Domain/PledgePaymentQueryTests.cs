using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.PledgePayments;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgePaymentQueryTests
{
    [Fact]
    public void Constructor_uses_default_pagination_and_descending_payment_date_order()
    {
        var query = new PledgePaymentQuery();

        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
        Assert.Equal(PledgePaymentSortBy.PaymentDate, query.SortBy);
        Assert.Equal(SortOrder.Descending, query.SortOrder);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public void Constructor_rejects_invalid_pagination(int page, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PledgePaymentQuery(page, pageSize));
    }

    [Theory]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.PaymentDate, SortOrder.Descending)]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.Amount, SortOrder.Descending)]
    public void Constructor_accepts_sort_options(PledgePaymentSortBy sortBy, SortOrder sortOrder)
    {
        var query = new PledgePaymentQuery(sortBy: sortBy, sortOrder: sortOrder);

        Assert.Equal(sortBy, query.SortBy);
        Assert.Equal(sortOrder, query.SortOrder);
    }

    [Theory]
    [InlineData((PledgePaymentSortBy)(-1), SortOrder.Ascending)]
    [InlineData((PledgePaymentSortBy)2, SortOrder.Ascending)]
    [InlineData(PledgePaymentSortBy.PaymentDate, (SortOrder)(-1))]
    [InlineData(PledgePaymentSortBy.PaymentDate, (SortOrder)2)]
    public void Constructor_rejects_unsupported_sorting(
        PledgePaymentSortBy sortBy, SortOrder sortOrder)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PledgePaymentQuery(sortBy: sortBy, sortOrder: sortOrder));
    }
}