using ICFAccounting.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace ICFAccounting.IntegrationTests.Data;

public sealed class PostgresModelFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17").Build();
    private readonly HashSet<string> _createdDatabases = [];
    private readonly object _databaseLock = new();

    public ICFAccountingDbContext CreateContext(string databaseName = "icfaccounting_model_tests")
    {
        EnsureDatabaseExists(databaseName);
        var connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName,
        };
        var options = new DbContextOptionsBuilder<ICFAccountingDbContext>()
            .UseNpgsql(connectionString.ConnectionString)
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

    private void EnsureDatabaseExists(string databaseName)
    {
        lock (_databaseLock)
        {
            if (_createdDatabases.Contains(databaseName))
            {
                return;
            }

            var adminConnectionString = new NpgsqlConnectionStringBuilder(
                _container.GetConnectionString()
            )
            {
                Database = "postgres",
            };
            using var connection = new NpgsqlConnection(adminConnectionString.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                $"CREATE DATABASE {new NpgsqlCommandBuilder().QuoteIdentifier(databaseName)}";
            command.ExecuteNonQuery();
            _createdDatabases.Add(databaseName);
        }
    }
}
