namespace ICFAccounting.Api.Models.PledgeModels;

/// <summary>
/// A page of pledges and the pagination metadata for the complete collection.
/// </summary>
public sealed record PledgePageResponse
{
    public required IReadOnlyList<PledgeResponse> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}