namespace ICFAccounting.Api.Models.PledgeModels;

/// <summary>
/// Request to update a pledge.
/// Nullable properties allow missing required fields to be rejected with a ProblemDetails response.
/// </summary>
public sealed record UpdatePledgeRequest
{
    public int? Year { get; init; }

    public decimal? Amount { get; init; }

    public Guid? PledgerId { get; init; }

    public Guid? PledgeCategoryId { get; init; }

    public string? Comment { get; init; }
}