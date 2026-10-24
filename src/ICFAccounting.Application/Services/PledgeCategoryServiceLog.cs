using ICFAccounting.Domain.PledgeCategories;
using Microsoft.Extensions.Logging;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Source-generated log messages for <see cref="PledgeCategoryService"/>.
/// </summary>
internal static partial class PledgeCategoryServiceLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Added pledge category {PledgeCategoryId}.")]
    public static partial void PledgeCategoryAdded(this ILogger logger, PledgeCategoryId pledgeCategoryId);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Retrieved pledge category {PledgeCategoryId}.")]
    public static partial void PledgeCategoryRetrieved(this ILogger logger, PledgeCategoryId pledgeCategoryId);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Pledge category {PledgeCategoryId} was not found.")]
    public static partial void PledgeCategoryNotFound(this ILogger logger, PledgeCategoryId pledgeCategoryId);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Retrieved {PledgeCategoryCount} pledge categories.")]
    public static partial void PledgeCategoriesRetrieved(this ILogger logger, int pledgeCategoryCount);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Updated pledge category {PledgeCategoryId}.")]
    public static partial void PledgeCategoryUpdated(this ILogger logger, PledgeCategoryId pledgeCategoryId);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Pledge category {PledgeCategoryId} was not found for update.")]
    public static partial void PledgeCategoryNotFoundForUpdate(this ILogger logger, PledgeCategoryId pledgeCategoryId);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Information, Message = "Removed pledge category {PledgeCategoryId}.")]
    public static partial void PledgeCategoryRemoved(this ILogger logger, PledgeCategoryId pledgeCategoryId);
}
