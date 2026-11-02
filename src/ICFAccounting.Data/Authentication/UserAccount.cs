using Microsoft.AspNetCore.Identity;

namespace ICFAccounting.Data.Authentication;

/// <summary>Administrator-provisioned credentials and initial profile information.</summary>
public sealed class UserAccount : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? ProfileImageUrl { get; set; }
}
