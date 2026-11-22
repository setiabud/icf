using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Reminder email sender implementation.
/// </summary>
public sealed class PledgeSummaryEmailSenderService(
    ILogger<PledgeSummaryEmailSenderService> logger
) : IPledgeSummaryEmailSenderService
{
    private readonly ILogger<PledgeSummaryEmailSenderService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public Task SendAsync(
        PledgerPledgeSummary summary,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(summary);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogWarning(
            "Email sending is not implemented. Pledge summary for {PledgerName} at {Email} was not sent.",
            summary.PledgerName,
            summary.Email
        );
        return Task.CompletedTask;
    }
}
