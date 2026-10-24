using System.Runtime.CompilerServices;

namespace ICFAccounting.Application.Services;

/// <summary>
/// Shared input checks for application services.
/// </summary>
internal static class ServiceGuard
{
    /// <summary>
    /// Throws <see cref="ArgumentException"/> when a strongly-typed ID has its default (empty) value.
    /// </summary>
    public static void ThrowIfEmptyId<TId>(
        TId id,
        string entityName,
        [CallerArgumentExpression(nameof(id))] string? paramName = null)
        where TId : struct, IEquatable<TId>
    {
        if (id.Equals(default))
        {
            throw new ArgumentException($"{entityName} ID cannot be empty.", paramName);
        }
    }
}
