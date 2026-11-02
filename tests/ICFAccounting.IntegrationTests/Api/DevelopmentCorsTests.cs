using Microsoft.AspNetCore.Hosting;

namespace ICFAccounting.IntegrationTests.Api;

public sealed class DevelopmentCorsTests
{
    private const string CONNECTION_STRING = "Server=localhost;Database=ICFAccounting;Integrated Security=True";
    private const string FRONTEND_ORIGIN = "https://localhost:5173";

    [Fact]
    public async Task Preflight_from_frontend_origin_is_allowed_in_development()
    {
        using var factory = new SqlServerApiFactory(CONNECTION_STRING);
        using var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();

        using var response = await client.SendAsync(CreatePreflight(FRONTEND_ORIGIN), CancellationToken.None);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal(FRONTEND_ORIGIN, Assert.Single(origins));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
        Assert.Contains("x-csrf-token", string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers")),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preflight_from_unknown_origin_is_not_allowed_in_development()
    {
        using var factory = new SqlServerApiFactory(CONNECTION_STRING);
        using var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();

        using var response = await client.SendAsync(CreatePreflight("https://example.com"), CancellationToken.None);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_from_frontend_origin_is_not_allowed_in_production()
    {
        using var factory = new SqlServerApiFactory(CONNECTION_STRING);
        using var client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();

        using var response = await client.SendAsync(CreatePreflight(FRONTEND_ORIGIN), CancellationToken.None);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static HttpRequestMessage CreatePreflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/pledgers");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,x-csrf-token");
        return request;
    }
}
