namespace ICFAccounting.Api.Models.PledgeModels;

/// <summary>
/// Pledge details returned by the API.
/// </summary>
public sealed record PledgeResponse
{
    public Guid Id { get; init; }

    public int Year { get; init; }

    public decimal Amount { get; init; }

    public Guid PledgerId { get; init; }

    public Guid PledgeCategoryId { get; init; }

    public string? Comment { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}