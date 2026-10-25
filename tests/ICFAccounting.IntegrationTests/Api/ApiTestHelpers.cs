using System.Net;
using System.Net.Http.Json;
using ICFAccounting.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ICFAccounting.IntegrationTests.Api;

internal static class ApiTestHelpers
{
    public static HttpClient CreateClient(
        WebApplicationFactory<Program> factory, bool allowAutoRedirect = true)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = allowAutoRedirect
        });
    }

    public static async Task<ProblemDetails> AssertProblemAsync(
        HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        return problem;
    }
}