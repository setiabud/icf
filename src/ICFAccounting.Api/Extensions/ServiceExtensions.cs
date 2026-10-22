using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Repositories;
using ICFAccounting.Application.Services;

namespace ICFAccounting.Api.Extensions;

/// <summary>
/// Extension methods for configuring application services
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Add Clean Architecture services to the DI container
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddSingleton<IWeatherForecastRepository, InMemoryWeatherForecastRepository>();

        // Register application services
        services.AddScoped<IWeatherService, WeatherService>();

        return services;
    }
}
