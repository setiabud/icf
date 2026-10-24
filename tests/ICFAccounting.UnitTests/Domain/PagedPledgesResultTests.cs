using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;

namespace ICFAccounting.UnitTests.Domain;

public sealed class PagedPledgesResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(int.MaxValue, 100, 21474837)]
    public void Create_calculates_total_pages(int totalCount, int pageSize, int totalPages)
    {
        var result = PagedPledgesResult.Create([], 3, pageSize, totalCount);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(totalCount, result.TotalCount);
        Assert.Equal(totalPages, result.TotalPages);
    }

    [Fact]
    public void Create_preserves_items_without_exposing_source_collection_mutations()
    {
        var pledge = Pledge.Create(2025, 100m, PledgerId.New(), PledgeCategoryId.New());
        var items = new List<Pledge> { pledge };

        var result = PagedPledgesResult.Create(items, 1, 10, 1);
        items.Clear();

        Assert.Same(pledge, Assert.Single(result.Items));
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 10, -1)]
    public void Create_rejects_invalid_metadata(int page, int pageSize, int totalCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PagedPledgesResult.Create([], page, pageSize, totalCount));
    }

    [Fact]
    public void Create_rejects_null_items()
    {
        Assert.Throws<ArgumentNullException>(() => PagedPledgesResult.Create(null!, 1, 10, 0));
    }
}
