namespace ICFAccounting.Api.Extensions;

/// <summary>
/// Extension methods for configuring logging
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configure application logging
    /// </summary>
    public static IServiceCollection AddApplicationLogging(this IServiceCollection services)
    {
        // Logging is already configured by default in WebApplication.CreateBuilder()
        // This extension is here for consistency and future enhancements
        return services;
    }

    /// <summary>
    /// Configure logging middleware
    /// </summary>
    public static WebApplication UseApplicationLogging(this WebApplication app)
    {
        return app;
    }
}
