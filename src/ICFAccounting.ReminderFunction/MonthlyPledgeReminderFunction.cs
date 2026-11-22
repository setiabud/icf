using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ICFAccounting.Application.Services;

namespace ICFAccounting.ReminderFunction;

public sealed class MonthlyPledgeReminderFunction(
    ILogger<MonthlyPledgeReminderFunction> logger,
    IPledgeSummaryService pledgeSummaryService,
    IPledgeSummaryEmailSender pledgeSummaryEmailSender)
{
    public const string Schedule = "0 0 0 5 * *";
    private readonly ILogger<MonthlyPledgeReminderFunction> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPledgeSummaryService _pledgeSummaryService =
        pledgeSummaryService ?? throw new ArgumentNullException(nameof(pledgeSummaryService));
    private readonly IPledgeSummaryEmailSender _pledgeSummaryEmailSender =
        pledgeSummaryEmailSender ?? throw new ArgumentNullException(nameof(pledgeSummaryEmailSender));

    [Function("MonthlyPledgeReminder")]
    public async Task Run(
        [TimerTrigger(Schedule)] TimerInfo timerInfo,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInformation(
            "Monthly pledge reminder function executed at: {ExecutionTime}",
            DateTimeOffset.UtcNow);

        if (timerInfo.ScheduleStatus is not null)
        {
            _logger.LogInformation(
                "Next scheduled execution: {NextExecution}",
                timerInfo.ScheduleStatus.Next);
        }

        var summaries = await _pledgeSummaryService.CreateAsync(cancellationToken);
        foreach (var summary in summaries)
        {
            await _pledgeSummaryEmailSender.SendAsync(summary, cancellationToken);
        }
        _logger.LogInformation("Created {SummaryCount} pledge summaries.", summaries.Count);
    }
}
