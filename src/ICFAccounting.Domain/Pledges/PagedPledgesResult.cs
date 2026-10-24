namespace ICFAccounting.Domain.Pledges;

/// <summary>
/// A page of pledges and pagination metadata for the complete collection.
/// </summary>
public sealed class PagedPledgesResult
{
    private PagedPledgesResult(IReadOnlyList<Pledge> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<Pledge> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages => (int)(((long)TotalCount + PageSize - 1) / PageSize);

    public static PagedPledgesResult Create(
        IReadOnlyList<Pledge> items, int page, int pageSize, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        return new PagedPledgesResult(Array.AsReadOnly(items.ToArray()), page, pageSize, totalCount);
    }
}
