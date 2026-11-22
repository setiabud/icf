namespace ICFAccounting.Application.Services;

/// <summary>
/// Sends pledge summaries to pledgers.
/// </summary>
public interface IPledgeSummaryEmailSenderService
{
    /// <summary>
    /// Sends a pledge summary asynchronously.
    /// </summary>
    Task SendAsync(PledgerPledgeSummary summary, CancellationToken cancellationToken = default);
}
