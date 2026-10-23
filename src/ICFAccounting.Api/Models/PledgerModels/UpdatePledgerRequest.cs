namespace ICFAccounting.Api.Models.PledgerModels;

/// <summary>
/// Request to update a pledger.
/// </summary>
public sealed record UpdatePledgerRequest
{
    public string? PledgerName { get; init; }

    public bool? Active { get; init; }
}
