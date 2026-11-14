using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ICFAccounting.Data.Extensions;

/// <summary>
/// Registers PostgreSQL persistence behind the domain repository interfaces.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a scoped accounting context and PostgreSQL repositories using the ICFAccounting connection string.
    /// </summary>
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration.GetConnectionString("ICFAccounting");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The ICFAccounting connection string is required. Set ConnectionStrings__ICFAccounting " +
                "or configure ConnectionStrings:ICFAccounting with .NET user-secrets for local development.");
        }

        services.AddDbContext<ICFAccountingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IPledgerRepository, PostgresPledgerRepository>();
        services.AddScoped<IPledgeCategoryRepository, PostgresPledgeCategoryRepository>();
        services.AddScoped<IPledgeRepository, PostgresPledgeRepository>();
        services.AddScoped<IPledgePaymentRepository, PostgresPledgePaymentRepository>();
        return services;
    }
}