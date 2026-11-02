using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ICFAccounting.Data.Authentication;

/// <summary>Creates the Identity context for explicit schema migrations.</summary>
public sealed class AuthenticationDbContextFactory : IDesignTimeDbContextFactory<AuthenticationDbContext>
{
    public AuthenticationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ICFAccounting");
        var options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__IdentityMigrationsHistory", "auth"))
            .Options;
        return new AuthenticationDbContext(options);
    }
}
