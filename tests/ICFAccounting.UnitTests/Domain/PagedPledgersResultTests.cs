using ICFAccounting.Domain.Pledgers;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PagedPledgersResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(int.MaxValue, 100, 21474837)]
    public void Create_calculates_total_pages(int totalCount, int pageSize, int totalPages)
    {
        var result = PagedPledgersResult.Create([], 3, pageSize, totalCount);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(totalPages, result.TotalPages);
    }

    [Fact]
    public void Create_preserves_items_without_exposing_source_collection_mutations()
    {
        var pledger = Pledger.Create("Community Fund");
        var items = new List<Pledger> { pledger };

        var result = PagedPledgersResult.Create(items, 1, 10, 1);
        items.Clear();

        Assert.Same(pledger, Assert.Single(result.Items));
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 10, -1)]
    public void Create_rejects_invalid_metadata(int page, int pageSize, int totalCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PagedPledgersResult.Create([], page, pageSize, totalCount));
    }

    [Fact]
    public void Create_rejects_null_items()
    {
        Assert.Throws<ArgumentNullException>(() => PagedPledgersResult.Create(null!, 1, 10, 0));
    }
}
