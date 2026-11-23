using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ICFAccounting.Application.Services;

namespace ICFAccounting.ReminderFunction;

public sealed class MonthlyPledgeReminderFunction(
    ILogger<MonthlyPledgeReminderFunction> logger,
    IPledgeSummaryService pledgeSummaryService,
    IPledgeSummarySheetWriterService pledgeSummarySheetWriter)
{
    public const string Schedule = "0 0 0 5 * *";
    private readonly ILogger<MonthlyPledgeReminderFunction> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPledgeSummaryService _pledgeSummaryService =
        pledgeSummaryService ?? throw new ArgumentNullException(nameof(pledgeSummaryService));
    private readonly IPledgeSummarySheetWriterService _pledgeSummarySheetWriter =
        pledgeSummarySheetWriter ?? throw new ArgumentNullException(nameof(pledgeSummarySheetWriter));

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
        await _pledgeSummarySheetWriter.WriteAsync(summaries, cancellationToken);
        _logger.LogInformation("Wrote {SummaryCount} pledge summaries to Google Sheet.", summaries.Count);
    }
}
