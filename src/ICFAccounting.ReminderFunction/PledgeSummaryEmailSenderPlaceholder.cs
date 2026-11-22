using ICFAccounting.Application.Services;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.ReminderFunction;

/// <summary>
/// Placeholder sender until the email provider is implemented.
/// </summary>
public sealed class PledgeSummaryEmailSenderPlaceholder(
    ILogger<PledgeSummaryEmailSenderPlaceholder> logger
) : IPledgeSummaryEmailSender
{
    private readonly ILogger<PledgeSummaryEmailSenderPlaceholder> _logger =
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
