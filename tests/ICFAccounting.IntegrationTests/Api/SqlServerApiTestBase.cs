using ICFAccounting.Api;
using ICFAccounting.Data;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ICFAccounting.IntegrationTests.Api;

/// <summary>
/// Gives each endpoint test its own migrated database while sharing the SQL container fixture.
/// </summary>
public abstract class SqlServerApiTestBase(SqlServerModelFixture fixture) : IAsyncLifetime
{
    private readonly string _databaseName = $"ICFAccountingEndpointTests_{Guid.NewGuid():N}";
    protected WebApplicationFactory<Program> _factory = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await SeedAsync(context);
        using var factoryContext = CreateContext();
        _factory = new SqlServerApiFactory(factoryContext.Database.GetDbConnection().ConnectionString);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    /// <summary>Creates another host for the same test database without replacing repositories.</summary>
    protected WebApplicationFactory<Program> CreateIsolatedFactory()
    {
        return _factory.WithWebHostBuilder(_ => { });
    }

    /// <summary>Provides the database context for test-only setup and independent verification.</summary>
    protected ICFAccountingDbContext CreateContext()
    {
        return fixture.CreateContext(_databaseName);
    }

    /// <summary>Seeds only the related aggregates required by the endpoint test class.</summary>
    protected virtual Task SeedAsync(ICFAccountingDbContext context)
    {
        return Task.CompletedTask;
    }
}