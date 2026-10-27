using ICFAccounting.Api.Extensions;
using ICFAccounting.Data;
using ICFAccounting.Data.Extensions;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ICFAccounting.IntegrationTests.Api;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void Api_startup_rejects_missing_connection_string_without_a_fallback()
    {
        using var factory = new SqlServerApiFactory(null);
        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("ConnectionStrings__ICFAccounting", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddDataServices_rejects_missing_connection_strings(string? connectionString)
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(connectionString);

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddDataServices(configuration));

        Assert.Contains("ConnectionStrings__ICFAccounting", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddDataServices_registers_only_scoped_sql_repositories_and_context()
    {
        var services = new ServiceCollection();
        services.AddDataServices(CreateConfiguration("Server=localhost;Database=ICFAccounting;Integrated Security=True"));

        AssertScopedRegistration<IPledgerRepository, SqlServerPledgerRepository>(services);
        AssertScopedRegistration<IPledgeCategoryRepository, SqlServerPledgeCategoryRepository>(services);
        AssertScopedRegistration<IPledgeRepository, SqlServerPledgeRepository>(services);
        AssertScopedRegistration<IPledgePaymentRepository, SqlServerPledgePaymentRepository>(services);
        Assert.Equal(ServiceLifetime.Scoped,
            Assert.Single(services, service => service.ServiceType == typeof(ICFAccountingDbContext)).Lifetime);
        Assert.DoesNotContain(services, service => service.ImplementationType?.Name.StartsWith("InMemory") == true);
    }

    [Fact]
    public void Application_and_data_services_resolve_with_scoped_lifetimes_and_configured_database()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataServices(CreateConfiguration("Server=localhost;Database=ICFAccounting;Integrated Security=True"));
        services.AddApplicationServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var first = firstScope.ServiceProvider;
        var second = secondScope.ServiceProvider;

        var context = first.GetRequiredService<ICFAccountingDbContext>();
        Assert.Same(context, first.GetRequiredService<ICFAccountingDbContext>());
        Assert.NotSame(context, second.GetRequiredService<ICFAccountingDbContext>());
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal("ICFAccounting", context.Database.GetDbConnection().Database);
        AssertScopeIsolation<IPledgerRepository, SqlServerPledgerRepository>(first, second);
        AssertScopeIsolation<IPledgeCategoryRepository, SqlServerPledgeCategoryRepository>(first, second);
        AssertScopeIsolation<IPledgeRepository, SqlServerPledgeRepository>(first, second);
        AssertScopeIsolation<IPledgePaymentRepository, SqlServerPledgePaymentRepository>(first, second);
        Assert.Same(TimeProvider.System, first.GetRequiredService<TimeProvider>());
        Assert.Same(first.GetRequiredService<TimeProvider>(), second.GetRequiredService<TimeProvider>());
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IPledgerRepository>());
    }

    private static IConfiguration CreateConfiguration(string? connectionString)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ICFAccounting"] = connectionString
        }).Build();
    }

    private static void AssertScopedRegistration<TService, TImplementation>(IServiceCollection services)
    {
        var descriptor = Assert.Single(services, service => service.ServiceType == typeof(TService));
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    private static void AssertScopeIsolation<TService, TImplementation>(IServiceProvider first, IServiceProvider second)
        where TService : class
    {
        var repository = Assert.Single(first.GetServices<TService>());
        Assert.IsType<TImplementation>(repository);
        Assert.Same(repository, first.GetRequiredService<TService>());
        Assert.NotSame(repository, second.GetRequiredService<TService>());
    }
}