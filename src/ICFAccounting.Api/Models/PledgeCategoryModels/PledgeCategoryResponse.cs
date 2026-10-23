namespace ICFAccounting.Api.Models.PledgeCategoryModels;

/// <summary>
/// Pledge category details returned by the API.
/// </summary>
public sealed record PledgeCategoryResponse
{
    public Guid Id { get; init; }

    public required string CategoryName { get; init; }

    public bool Active { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
