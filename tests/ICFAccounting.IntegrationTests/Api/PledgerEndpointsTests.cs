using System.Net;
using System.Net.Http.Json;
using ICFAccounting.Api;
using ICFAccounting.Api.Models;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ICFAccounting.IntegrationTests.Api;

public sealed class PledgerEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PledgerEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_with_required_fields_returns_created_details_and_location()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        var request = new CreatePledgerRequest { PledgerName = "Community Fund" };
        var beforeRequest = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync("/pledgers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var pledger = await response.Content.ReadFromJsonAsync<PledgerResponse>();
        Assert.NotNull(pledger);
        Assert.NotEqual(Guid.Empty, pledger.Id);
        Assert.Equal(request.PledgerName, pledger.PledgerName);
        Assert.True(pledger.Active);
        Assert.Equal(DateTimeKind.Utc, pledger.CreatedAt.Kind);
        Assert.InRange(pledger.CreatedAt, beforeRequest, DateTime.UtcNow);
        Assert.Equal(pledger.CreatedAt, pledger.UpdatedAt);
        Assert.Equal($"/pledgers/{pledger.Id}", response.Headers.Location?.OriginalString);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var storedPledger = await repository.GetByIdAsync(new PledgerId(pledger.Id), CancellationToken.None);
        Assert.NotNull(storedPledger);
        Assert.Equal(pledger.PledgerName, storedPledger.PledgerName);
        Assert.Equal(pledger.Active, storedPledger.Active);
        Assert.Equal(pledger.CreatedAt, storedPledger.CreatedAt);
        Assert.Equal(pledger.UpdatedAt, storedPledger.UpdatedAt);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"pledgerName":null}""")]
    [InlineData("""{"pledgerName":""}""")]
    [InlineData("""{"pledgerName":"   "}""")]
    [InlineData("""{"pledgerName":"\t\r\n"}""")]
    public async Task Post_with_invalid_name_returns_bad_request_problem_details(string json)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var originalCount = (await repository.GetAllAsync(CancellationToken.None)).TotalCount;
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/pledgers", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.Null(response.Headers.Location);
        Assert.Equal(originalCount, (await repository.GetAllAsync(CancellationToken.None)).TotalCount);
    }

    [Fact]
    public async Task Get_without_query_returns_default_sorted_page_and_response_details()
    {
        using var factory = CreateIsolatedFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var pledgers = Enumerable.Range(1, 12)
            .Select(number => Pledger.Create($"Pledger {number:D2}")).ToArray();
        foreach (var pledger in pledgers.Reverse())
        {
            await repository.AddAsync(pledger, CancellationToken.None);
        }

        using var response = await client.GetAsync("/pledgers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgerPageResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(pledgers.Take(10).Select(pledger => pledger.PledgerName),
            result.Items.Select(pledger => pledger.PledgerName));
        Assert.Equal(pledgers[0].Id.Value, result.Items[0].Id);
        Assert.Equal(pledgers[0].Active, result.Items[0].Active);
        Assert.Equal(pledgers[0].CreatedAt, result.Items[0].CreatedAt);
        Assert.Equal(pledgers[0].UpdatedAt, result.Items[0].UpdatedAt);
    }

    [Theory]
    [InlineData("?page=2&pageSize=2", "Charlie,delta", 2, 2, 3)]
    [InlineData("?page=1&pageSize=2&sortBy=pledgername&sortOrder=DESCENDING", "Echo,delta", 1, 2, 3)]
    [InlineData("?page=2&pageSize=2&sortOrder=descending", "Charlie,bravo", 2, 2, 3)]
    [InlineData("?pageSize=100&sortOrder=ASCENDING", "Alpha,bravo,Charlie,delta,Echo", 1, 100, 1)]
    [InlineData("?page=3&pageSize=2", "Echo", 3, 2, 3)]
    [InlineData("?page=1&pageSize=5", "Alpha,bravo,Charlie,delta,Echo", 1, 5, 1)]
    [InlineData("?page=4&pageSize=2", "", 4, 2, 3)]
    [InlineData("?page=2147483647&pageSize=100", "", int.MaxValue, 100, 1)]
    public async Task Get_with_query_returns_sorted_page(
        string query, string expectedNames, int page, int pageSize, int totalPages)
    {
        using var factory = CreateIsolatedFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        foreach (var name in new[] { "Charlie", "Echo", "Alpha", "delta", "bravo" })
        {
            await repository.AddAsync(Pledger.Create(name), CancellationToken.None);
        }

        using var response = await client.GetAsync($"/pledgers{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgerPageResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedNames, string.Join(",", result.Items.Select(pledger => pledger.PledgerName)));
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(totalPages, result.TotalPages);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=-1")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=-1")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=abc")]
    [InlineData("?page=2147483648")]
    [InlineData("?pageSize=abc")]
    [InlineData("?sortBy=Id")]
    [InlineData("?sortBy=")]
    [InlineData("?sortOrder=random")]
    [InlineData("?sortOrder=")]
    [InlineData("?sortOrder=asc")]
    [InlineData("?sortOrder=desc")]
    [InlineData("?sortOrder=0")]
    [InlineData("?sortOrder=1")]
    [InlineData("?sortOrder=2")]
    [InlineData("?sortBy=0")]
    [InlineData("?sortBy=1")]
    [InlineData("?sortOrder=Ascending,Descending")]
    public async Task Get_with_invalid_query_returns_bad_request_problem_details(string query)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync($"/pledgers{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }

    [Theory]
    [InlineData("", 1, 10)]
    [InlineData("?page=3&pageSize=5", 3, 5)]
    public async Task Get_with_empty_repository_returns_empty_page(string query, int page, int pageSize)
    {
        using var factory = CreateIsolatedFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync($"/pledgers{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgerPageResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task Get_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgerRepository>(new FailingPledgerRepository())));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync("/pledgers");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledgers could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", await response.Content.ReadAsStringAsync());
    }

    private WebApplicationFactory<Program> CreateIsolatedFactory()
    {
        return _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgerRepository, InMemoryPledgerRepository>()));
    }

    private sealed class FailingPledgerRepository : IPledgerRepository
    {
        public Task<int> CountAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<PagedPledgersResult> GetAllAsync(
            CancellationToken cancellationToken, PledgerQuery? query = null)
            => throw new InvalidOperationException("Internal repository failure");

        public Task AddAsync(Pledger pledger, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task RemoveAsync(PledgerId id, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}
