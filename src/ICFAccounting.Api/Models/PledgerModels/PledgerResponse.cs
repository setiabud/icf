namespace ICFAccounting.Api.Models.PledgerModels;

/// <summary>
/// Pledger details returned by the API.
/// </summary>
public sealed record PledgerResponse
{
    public Guid Id { get; init; }

    public required string PledgerName { get; init; }

    public bool Active { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }
}
