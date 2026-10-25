using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Source-generated log messages for <see cref="PledgeService"/>.
/// </summary>
internal static partial class PledgeServiceLog
{
    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Added pledge {PledgeId}.")]
    public static partial void PledgeAdded(this ILogger logger, PledgeId pledgeId);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Retrieved pledge {PledgeId}.")]
    public static partial void PledgeRetrieved(this ILogger logger, PledgeId pledgeId);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "Pledge {PledgeId} was not found.")]
    public static partial void PledgeNotFound(this ILogger logger, PledgeId pledgeId);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message = "Retrieved {PledgeCount} pledges for pledger {PledgerId}.")]
    public static partial void PledgesByPledgerRetrieved(
        this ILogger logger, PledgerId pledgerId, int pledgeCount);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message = "Retrieved {PledgeCount} pledges for category {PledgeCategoryId}.")]
    public static partial void PledgesByCategoryRetrieved(
        this ILogger logger, PledgeCategoryId pledgeCategoryId, int pledgeCount);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information, Message = "Updated pledge {PledgeId}.")]
    public static partial void PledgeUpdated(this ILogger logger, PledgeId pledgeId);

    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Information,
        Message = "Pledge {PledgeId} was not found for update.")]
    public static partial void PledgeNotFoundForUpdate(this ILogger logger, PledgeId pledgeId);

    [LoggerMessage(EventId = 3008, Level = LogLevel.Information, Message = "Removed pledge {PledgeId}.")]
    public static partial void PledgeRemoved(this ILogger logger, PledgeId pledgeId);
}