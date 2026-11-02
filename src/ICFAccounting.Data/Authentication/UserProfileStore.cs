using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ICFAccounting.Data.Authentication;

/// <summary>Creates a single application profile per verified account, including concurrent first sign-ins.</summary>
public sealed class UserProfileStore(AuthenticationDbContext database)
{
    public async Task<UserProfile?> FindAsync(string accountId, CancellationToken cancellationToken) =>
        await database.UserProfiles.FindAsync([accountId], cancellationToken);

    public async Task<UserProfile> GetOrCreateAsync(UserAccount account, CancellationToken cancellationToken)
    {
        var existing = await database.UserProfiles.FindAsync([account.Id], cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var profile = new UserProfile
        {
            Id = account.Id,
            Name = account.DisplayName ?? account.Email ?? "Member",
            ProfileImageUrl = account.ProfileImageUrl,
            CreatedAt = DateTimeOffset.UtcNow
        };
        database.UserProfiles.Add(profile);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return profile;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            database.Entry(profile).State = EntityState.Detached;
            return await database.UserProfiles.SingleAsync(user => user.Id == account.Id, cancellationToken);
        }
    }
}
