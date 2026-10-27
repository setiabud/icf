namespace ICFAccounting.Domain.Repositories;

/// <summary>
/// Indicates that a persistence operation conflicts with related accounting records.
/// </summary>
public sealed class RelationshipConflictException : Exception
{
    /// <summary>
    /// Creates a conflict with a client-safe message while retaining the underlying failure.
    /// </summary>
    public RelationshipConflictException(Exception innerException)
        : base("The operation conflicts with related accounting records.", innerException)
    {
    }
}