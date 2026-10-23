using System.Net;
using System.Net.Http.Json;
using ICFAccounting.Api;
using ICFAccounting.Api.Models.PledgeCategoryModels;
using ICFAccounting.Application.Services;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ICFAccounting.IntegrationTests.Api;

public sealed class PledgeCategoryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PledgeCategoryEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_by_id_with_existing_id_returns_pledge_category()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        var pledgeCategory = PledgeCategory.Create("Building Fund");
        await repository.AddAsync(pledgeCategory, CancellationToken.None);

        using var response = await client.GetAsync($"/pledge-categories/{pledgeCategory.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgeCategoryResponse>();
        Assert.NotNull(result);
        Assert.Equal(pledgeCategory.Id.Value, result.Id);
        Assert.Equal("Building Fund", result.CategoryName);
        Assert.True(result.Active);
        Assert.Equal(pledgeCategory.CreatedAt, result.CreatedAt);
        Assert.Equal(pledgeCategory.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task Get_by_id_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledge-categories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    public async Task Get_by_id_with_non_guid_id_returns_not_found(string id)
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledge-categories/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledge-categories/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category ID", problem.Title);
    }

    [Fact]
    public async Task Get_by_id_when_service_rejects_argument_returns_bad_request_problem_details()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IPledgeCategoryService>(_ => new RejectingPledgeCategoryService())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledge-categories/{Guid.NewGuid()}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category ID", problem.Title);
        Assert.Equal(RejectingPledgeCategoryService.RejectionMessage, problem.Detail);
    }

    [Fact]
    public async Task Get_by_id_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeCategoryRepository>(new FailingPledgeCategoryRepository())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledge-categories/{Guid.NewGuid()}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge category could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static async Task<ProblemDetails> AssertProblemAsync(
        HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        return problem;
    }

    private WebApplicationFactory<Program> CreateIsolatedFactory()
    {
        return _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeCategoryRepository, InMemoryPledgeCategoryRepository>()));
    }

    private sealed class FailingPledgeCategoryRepository : IPledgeCategoryRepository
    {
        public Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task UpdateAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");
    }

    private sealed class RejectingPledgeCategoryService : IPledgeCategoryService
    {
        public const string RejectionMessage = "Pledge category ID is not valid.";

        public Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage);

        public Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage, nameof(id));

        public Task AddAsync(PledgeCategory pledgeCategory, CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage, nameof(pledgeCategory));

        public Task<PledgeCategory?> UpdateAsync(
            PledgeCategoryId id, string categoryName, bool active, CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage, nameof(categoryName));

        public Task RemoveAsync(PledgeCategoryId id, CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage, nameof(id));
    }
}
