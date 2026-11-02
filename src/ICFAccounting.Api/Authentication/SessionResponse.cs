namespace ICFAccounting.Api.Authentication;

public sealed record SessionResponse(string Id, string Name, string Email, string? ProfileImageUrl, DateTimeOffset ExpiresAt);
