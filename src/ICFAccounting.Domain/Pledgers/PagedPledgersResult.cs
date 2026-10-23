namespace ICFAccounting.Domain.Pledgers;

/// <summary>
/// A page of pledgers and pagination metadata for the complete collection.
/// </summary>
public sealed class PagedPledgersResult
{
    private PagedPledgersResult(IReadOnlyList<Pledger> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<Pledger> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages => (int)(((long)TotalCount + PageSize - 1) / PageSize);

    public static PagedPledgersResult Create(
        IReadOnlyList<Pledger> items, int page, int pageSize, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        return new PagedPledgersResult(Array.AsReadOnly(items.ToArray()), page, pageSize, totalCount);
    }
}
