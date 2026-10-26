namespace ICFAccounting.Api.Models.PledgePaymentModels;

/// <summary>
/// A page of pledge payments and the pagination metadata for the complete collection.
/// </summary>
public sealed record PledgePaymentPageResponse
{
    public required IReadOnlyList<PledgePaymentResponse> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}
