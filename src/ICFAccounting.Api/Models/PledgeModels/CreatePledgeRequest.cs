namespace ICFAccounting.Api.Models.PledgeModels;

/// <summary>
/// Request to create a pledge.
/// </summary>
public sealed record CreatePledgeRequest
{
    public required int Year { get; init; }

    public required decimal Amount { get; init; }

    public required Guid PledgerId { get; init; }

    public required Guid PledgeCategoryId { get; init; }

    public string? Comment { get; init; }
}