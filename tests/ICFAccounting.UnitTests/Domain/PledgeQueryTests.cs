using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgeQueryTests
{
    [Fact]
    public void Constructor_uses_default_pagination_and_descending_order()
    {
        var query = new PledgeQuery();

        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
        Assert.Equal(PledgeSortBy.Year, query.SortBy);
        Assert.Equal(SortOrder.Descending, query.SortOrder);
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public void Constructor_accepts_sort_options(SortOrder sortOrder)
    {
        var query = new PledgeQuery(sortBy: PledgeSortBy.Year, sortOrder: sortOrder);

        Assert.Equal(PledgeSortBy.Year, query.SortBy);
        Assert.Equal(sortOrder, query.SortOrder);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    public void Constructor_rejects_invalid_pagination(int page, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PledgeQuery(page, pageSize));
    }

    [Theory]
    [InlineData((PledgeSortBy)(-1), SortOrder.Ascending)]
    [InlineData((PledgeSortBy)1, SortOrder.Ascending)]
    [InlineData(PledgeSortBy.Year, (SortOrder)(-1))]
    [InlineData(PledgeSortBy.Year, (SortOrder)2)]
    public void Constructor_rejects_unsupported_sorting(PledgeSortBy sortBy, SortOrder sortOrder)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PledgeQuery(sortBy: sortBy, sortOrder: sortOrder));
    }
}
