namespace ICFAccounting.Application.Services;

/// <summary>
/// Creates pledge summaries for pledgers who have email notifications enabled.
/// </summary>
public interface IPledgeSummaryService
{
    /// <summary>
    /// Creates one email-friendly summary for each eligible pledger.
    /// </summary>
    Task<IReadOnlyList<PledgerPledgeSummary>> CreateAsync(
        CancellationToken cancellationToken = default
    );
}
