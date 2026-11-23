namespace ICFAccounting.Application.Services;

/// <summary>
/// Replaces a Google Sheet with pledge summaries.
/// </summary>
public interface IPledgeSummarySheetWriterService
{
    /// <summary>
    /// Replaces the configured pledge-summary sheet asynchronously.
    /// </summary>
    Task WriteAsync(
        IReadOnlyList<PledgerPledgeSummary> summaries,
        CancellationToken cancellationToken = default
    );
}
