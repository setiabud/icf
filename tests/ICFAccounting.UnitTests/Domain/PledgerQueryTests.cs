using ICFAccounting.Domain.Common;
using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PledgerQueryTests
{
    [Fact]
    public void Constructor_uses_default_pagination_and_ascending_order()
    {
        var query = new PledgerQuery();

        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
        Assert.Equal(PledgerSortBy.PledgerName, query.SortBy);
        Assert.Equal(SortOrder.Ascending, query.SortOrder);
    }

    [Theory]
    [InlineData(SortOrder.Ascending)]
    [InlineData(SortOrder.Descending)]
    public void Constructor_accepts_sort_options(SortOrder sortOrder)
    {
        var query = new PledgerQuery(sortBy: PledgerSortBy.PledgerName, sortOrder: sortOrder);

        Assert.Equal(PledgerSortBy.PledgerName, query.SortBy);
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
        Assert.Throws<ArgumentOutOfRangeException>(() => new PledgerQuery(page, pageSize));
    }

    [Theory]
    [InlineData((PledgerSortBy)(-1), SortOrder.Ascending)]
    [InlineData((PledgerSortBy)1, SortOrder.Ascending)]
    [InlineData(PledgerSortBy.PledgerName, (SortOrder)(-1))]
    [InlineData(PledgerSortBy.PledgerName, (SortOrder)2)]
    public void Constructor_rejects_unsupported_sorting(PledgerSortBy sortBy, SortOrder sortOrder)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PledgerQuery(sortBy: sortBy, sortOrder: sortOrder));
    }
}
