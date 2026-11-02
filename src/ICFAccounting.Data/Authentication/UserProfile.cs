namespace ICFAccounting.Data.Authentication;

/// <summary>Application identity created only after successful credential verification.</summary>
public sealed class UserProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? ProfileImageUrl { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
