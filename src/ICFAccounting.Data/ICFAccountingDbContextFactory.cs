using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ICFAccounting.Data;

/// <summary>
/// Creates the design-time context for explicit EF commands independently of API startup.
/// </summary>
public sealed class ICFAccountingDbContextFactory : IDesignTimeDbContextFactory<ICFAccountingDbContext>
{
    /// <inheritdoc />
    public ICFAccountingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ICFAccounting");
        var options = new DbContextOptionsBuilder<ICFAccountingDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ICFAccountingDbContext(options);
    }
}