using ICFAccounting.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace ICFAccounting.IntegrationTests.Data;

public sealed class SqlServerModelFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-CU24-ubuntu-22.04").Build();

    public ICFAccountingDbContext CreateContext(string databaseName = "ICFAccountingModelTests")
    {
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = databaseName
        };
        var options = new DbContextOptionsBuilder<ICFAccountingDbContext>()
            .UseSqlServer(connectionString.ConnectionString)
            .Options;
        return new ICFAccountingDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}