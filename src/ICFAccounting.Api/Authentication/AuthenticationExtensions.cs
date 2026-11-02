using System.Threading.RateLimiting;
using ICFAccounting.Data.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ICFAccounting.Api.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAccountAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthenticationDbContext>(options => options.UseSqlServer(
            configuration.GetConnectionString("ICFAccounting"),
            sql => sql.MigrationsHistoryTable("__IdentityMigrationsHistory", "auth")));
        services.AddScoped<UserProfileStore>();
        services.AddIdentityCore<UserAccount>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
            .AddEntityFrameworkStores<AuthenticationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "__Host-ICFAccounting.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = false;
            options.Events.OnValidatePrincipal = async context =>
            {
                await SecurityStampValidator.ValidatePrincipalAsync(context);
                // Stamp checks must not turn the absolute eight-hour session into a sliding session.
                context.ShouldRenew = false;
            };
            options.Events.OnRedirectToLogin = context => WriteAuthenticationProblemAsync(context.HttpContext, 401);
            options.Events.OnRedirectToAccessDenied = context => WriteAuthenticationProblemAsync(context.HttpContext, 403);
        });
        var dataProtection = services.AddDataProtection().SetApplicationName("ICFAccounting");
        var keyRingPath = configuration["DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(keyRingPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyRingPath));
        }
        services.AddAuthorizationBuilder().SetFallbackPolicy(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build());
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-ICFAccounting.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await Results.Problem(statusCode: 429, title: "Too many sign-in attempts",
                    detail: "Wait a minute before trying again.").ExecuteAsync(context.HttpContext);
            };
        });
        return services;
    }

    public static WebApplication UseAccountAuthentication(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.Pragma = "no-cache";
            await next(context);
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.Use(async (context, next) =>
        {
            if (context.GetEndpoint() is not null &&
                !HttpMethods.IsGet(context.Request.Method) &&
                !HttpMethods.IsHead(context.Request.Method) &&
                !HttpMethods.IsOptions(context.Request.Method))
            {
                try
                {
                    await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    await Results.Problem(statusCode: 400, title: "Security token is invalid",
                        detail: "Refresh the page and try again.").ExecuteAsync(context);
                    return;
                }
            }
            await next(context);
        });
        return app;
    }

    private static Task WriteAuthenticationProblemAsync(HttpContext context, int status) =>
        Results.Problem(statusCode: status,
            title: status == 401 ? "Sign-in required" : "Access denied",
            detail: status == 401 ? "Your session is missing or expired. Sign in to continue." : "This request is not permitted.")
            .ExecuteAsync(context);
}
