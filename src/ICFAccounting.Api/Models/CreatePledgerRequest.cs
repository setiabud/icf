namespace ICFAccounting.Api.Models;

/// <summary>
/// Request to create a pledger.
/// </summary>
public sealed record CreatePledgerRequest
{
    public required string PledgerName { get; init; }
}
