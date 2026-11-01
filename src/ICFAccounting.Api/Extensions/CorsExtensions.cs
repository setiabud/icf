namespace ICFAccounting.Api.Extensions;

/// <summary>
/// Extension methods for the Development-only CORS policy used by the local web frontend.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Name of the Development-only CORS policy.
    /// </summary>
    public const string DEVELOPMENT_POLICY_NAME = "DevelopmentFrontend";

    /// <summary>
    /// Configuration key listing the origins allowed in Development.
    /// </summary>
    public const string DEVELOPMENT_ORIGINS_KEY = "Cors:DevelopmentOrigins";

    private static readonly string[] DefaultDevelopmentOrigins = ["http://localhost:5173"];

    /// <summary>
    /// Registers a CORS policy for the local frontend origins, only in the Development environment.
    /// </summary>
    public static IServiceCollection AddDevelopmentCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return services;
        }

        var configuredOrigins = configuration.GetSection(DEVELOPMENT_ORIGINS_KEY).Get<string[]>();
        var origins = configuredOrigins is { Length: > 0 } ? configuredOrigins : DefaultDevelopmentOrigins;

        services.AddCors(options => options.AddPolicy(DEVELOPMENT_POLICY_NAME, policy => policy
            .WithOrigins(origins)
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .WithHeaders("Content-Type", "Accept")
            .WithExposedHeaders("Location")));

        return services;
    }

    /// <summary>
    /// Applies the Development-only CORS policy. Has no effect outside Development.
    /// </summary>
    public static WebApplication UseDevelopmentCors(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(DEVELOPMENT_POLICY_NAME);
        }

        return app;
    }
}
