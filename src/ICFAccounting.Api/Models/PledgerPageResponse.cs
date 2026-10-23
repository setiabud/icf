namespace ICFAccounting.Api.Models;

/// <summary>
/// A page of pledgers and the pagination metadata for the complete collection.
/// </summary>
public sealed record PledgerPageResponse
{
    public required IReadOnlyList<PledgerResponse> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}
