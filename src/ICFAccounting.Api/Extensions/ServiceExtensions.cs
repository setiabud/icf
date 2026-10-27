using ICFAccounting.Application.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.TryAddSingleton(TimeProvider.System);

        // Register application services
        services.AddScoped<IPledgerService, PledgerService>();
        services.AddScoped<IPledgeCategoryService, PledgeCategoryService>();
        services.AddScoped<IPledgeService, PledgeService>();
        services.AddScoped<IPledgePaymentService, PledgePaymentService>();

        services.AddProblemDetails();
        services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(options =>
            options.ThrowOnBadRequest = false);

        return services;
    }
}
