using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ICFAccounting.Data.Authentication;

/// <summary>Isolates Identity persistence from the accounting aggregate model.</summary>
public sealed class AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options)
    : IdentityUserContext<UserAccount>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("auth");
        builder.Entity<UserAccount>(account =>
        {
            account.Property(user => user.DisplayName).HasMaxLength(200);
            account.Property(user => user.ProfileImageUrl).HasMaxLength(2048);
            account.HasIndex(user => user.NormalizedEmail).IsUnique();
        });
        builder.Entity<UserProfile>(profile =>
        {
            profile.Property(user => user.Name).HasMaxLength(256);
            profile.Property(user => user.ProfileImageUrl).HasMaxLength(2048);
            profile.HasOne<UserAccount>().WithOne().HasForeignKey<UserProfile>(user => user.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
