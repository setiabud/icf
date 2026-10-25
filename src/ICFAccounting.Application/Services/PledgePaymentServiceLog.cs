using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Source-generated log messages for <see cref="PledgePaymentService"/>.
/// </summary>
internal static partial class PledgePaymentServiceLog
{
    [LoggerMessage(EventId = 4001, Level = LogLevel.Information, Message = "Added pledge payment {PledgePaymentId}.")]
    public static partial void PledgePaymentAdded(this ILogger logger, PledgePaymentId pledgePaymentId);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "Retrieved pledge payment {PledgePaymentId}.")]
    public static partial void PledgePaymentRetrieved(this ILogger logger, PledgePaymentId pledgePaymentId);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Information,
        Message = "Pledge payment {PledgePaymentId} was not found.")]
    public static partial void PledgePaymentNotFound(this ILogger logger, PledgePaymentId pledgePaymentId);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Information,
        Message = "Retrieved {PledgePaymentCount} payments for pledge {PledgeId}.")]
    public static partial void PledgePaymentsByPledgeRetrieved(
        this ILogger logger, PledgeId pledgeId, int pledgePaymentCount);

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Information,
        Message = "Updated pledge payment {PledgePaymentId}.")]
    public static partial void PledgePaymentUpdated(this ILogger logger, PledgePaymentId pledgePaymentId);

    [LoggerMessage(
        EventId = 4006,
        Level = LogLevel.Information,
        Message = "Pledge payment {PledgePaymentId} was not found for update.")]
    public static partial void PledgePaymentNotFoundForUpdate(this ILogger logger, PledgePaymentId pledgePaymentId);

    [LoggerMessage(EventId = 4007, Level = LogLevel.Information, Message = "Removed pledge payment {PledgePaymentId}.")]
    public static partial void PledgePaymentRemoved(this ILogger logger, PledgePaymentId pledgePaymentId);
}