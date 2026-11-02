using ICFAccounting.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ICFAccounting.IntegrationTests.Api;

/// <summary>
/// Supplies isolated configuration without replacing the API's SQL repository registrations.
/// </summary>
public sealed class SqlServerApiFactory(string? connectionString) : WebApplicationFactory<Program>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:ICFAccounting", connectionString);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ICFAccounting"] = connectionString
            }));
        builder.ConfigureServices(services =>
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestSelector";
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            })
            .AddPolicyScheme("TestSelector", null, options =>
                options.ForwardDefaultSelector = context => context.Request.Headers.ContainsKey("X-Test-Authenticated")
                    ? TestAuthenticationHandler.SCHEME : IdentityConstants.ApplicationScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SCHEME, _ => { }));
    }
}