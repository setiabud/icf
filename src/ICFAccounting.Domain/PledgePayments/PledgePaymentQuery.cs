using ICFAccounting.Domain.Common;

namespace ICFAccounting.Domain.PledgePayments;

/// <summary>
/// Pagination and sorting options for pledge payment lists.
/// </summary>
public sealed class PledgePaymentQuery
{
    public PledgePaymentQuery(
        int page = 1,
        int pageSize = 20,
        PledgePaymentSortBy sortBy = PledgePaymentSortBy.PaymentDate,
        SortOrder sortOrder = SortOrder.Descending)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, 100);

        if (!Enum.IsDefined(sortBy))
        {
            throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported pledge payment sort field.");
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

    public PledgePaymentSortBy SortBy { get; }

    public SortOrder SortOrder { get; }
}