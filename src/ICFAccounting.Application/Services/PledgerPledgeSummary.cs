namespace ICFAccounting.Application.Services;

/// <summary>
/// A pledge summary for one pledger.
/// </summary>
public sealed record PledgerPledgeSummary(
    string PledgerName,
    string Email,
    string Html
);
