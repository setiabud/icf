using ICFAccounting.Domain.Common;

namespace ICFAccounting.Domain.Pledgers;

public sealed class PledgerQuery
{
    public PledgerQuery(
        int page = 1,
        int pageSize = 20,
        PledgerSortBy sortBy = PledgerSortBy.PledgerName,
        SortOrder sortOrder = SortOrder.Ascending)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);

        if (!Enum.IsDefined(sortBy))
        {
            throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported pledger sort field.");
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

    public PledgerSortBy SortBy { get; }

    public SortOrder SortOrder { get; }
}
