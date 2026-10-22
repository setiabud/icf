namespace ICFAccounting.Api.Extensions;

/// <summary>
/// Extension methods for configuring OpenAPI documentation
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Add OpenAPI services and configuration
    /// </summary>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi();
        return services;
    }
    
    /// <summary>
    /// Configure OpenAPI middleware pipeline
    /// </summary>
    public static WebApplication UseOpenApiDocumentation(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        
        return app;
    }
}
