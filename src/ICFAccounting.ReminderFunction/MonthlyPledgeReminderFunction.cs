using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.ReminderFunction;

public sealed class MonthlyPledgeReminderFunction(
    ILogger<MonthlyPledgeReminderFunction> logger)
{
    public const string Schedule = "0 0 0 5 * *";

    [Function("MonthlyPledgeReminder")]
    public void Run([TimerTrigger(Schedule)] TimerInfo timerInfo)
    {
        logger.LogInformation(
            "Monthly pledge reminder function executed at: {ExecutionTime}",
            DateTimeOffset.UtcNow);

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation(
                "Next scheduled execution: {NextExecution}",
                timerInfo.ScheduleStatus.Next);
        }
    }
}
