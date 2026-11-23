namespace ICFAccounting.Application.Services;

/// <summary>
/// Creates pledge summaries for active pledgers who have notification enabled and an email address.
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
