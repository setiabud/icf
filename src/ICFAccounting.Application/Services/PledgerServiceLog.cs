using ICFAccounting.Domain.Pledgers;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Source-generated log messages for <see cref="PledgerService"/>.
/// </summary>
internal static partial class PledgerServiceLog
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Added pledger {PledgerId}.")]
    public static partial void PledgerAdded(this ILogger logger, PledgerId pledgerId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Retrieved pledger {PledgerId}.")]
    public static partial void PledgerRetrieved(this ILogger logger, PledgerId pledgerId);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Pledger {PledgerId} was not found.")]
    public static partial void PledgerNotFound(this ILogger logger, PledgerId pledgerId);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Retrieved {PledgerCount} pledgers.")]
    public static partial void PledgersRetrieved(this ILogger logger, int pledgerCount);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Updated pledger {PledgerId}.")]
    public static partial void PledgerUpdated(this ILogger logger, PledgerId pledgerId);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Pledger {PledgerId} was not found for update.")]
    public static partial void PledgerNotFoundForUpdate(this ILogger logger, PledgerId pledgerId);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "Removed pledger {PledgerId}.")]
    public static partial void PledgerRemoved(this ILogger logger, PledgerId pledgerId);
}
