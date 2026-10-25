namespace ICFAccounting.Domain.PledgePayments;

/// <summary>
/// A page of pledge payments and pagination metadata for the complete collection.
/// </summary>
public sealed class PagedPledgePaymentsResult
{
    private PagedPledgePaymentsResult(
        IReadOnlyList<PledgePayment> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<PledgePayment> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages => (int)(((long)TotalCount + PageSize - 1) / PageSize);

    public static PagedPledgePaymentsResult Create(
        IReadOnlyList<PledgePayment> items, int page, int pageSize, int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(totalCount);

        return new PagedPledgePaymentsResult(Array.AsReadOnly(items.ToArray()), page, pageSize, totalCount);
    }
}