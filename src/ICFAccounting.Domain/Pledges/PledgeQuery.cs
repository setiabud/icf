using ICFAccounting.Domain.Common;

namespace ICFAccounting.Domain.Pledges;

/// <summary>
/// Pagination and sorting options for pledge lists.
/// </summary>
public sealed class PledgeQuery
{
    public PledgeQuery(
        int page = 1,
        int pageSize = 20,
        PledgeSortBy sortBy = PledgeSortBy.Year,
        SortOrder sortOrder = SortOrder.Descending)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);

        if (!Enum.IsDefined(sortBy))
        {
            throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported pledge sort field.");
        }

        if (!Enum.IsDefined(sortOrder))
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, "Unsupported sort order.");
        }

        Page = page;
        PageSize = pageSize;
        SortBy = sortBy;
        SortOrder = sortOrder;
    }

    public int Page { get; }

    public int PageSize { get; }

    public PledgeSortBy SortBy { get; }

    public SortOrder SortOrder { get; }
}
