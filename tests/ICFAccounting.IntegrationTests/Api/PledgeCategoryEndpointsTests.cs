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
    public async Task Post_returns_created_category_with_retrievable_location()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        var beforeRequest = DateTime.UtcNow;
        var request = new CreatePledgeCategoryRequest { CategoryName = "Building Fund" };

        using var response = await client.PostAsJsonAsync("/pledge-categories", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var result = await response.Content.ReadFromJsonAsync<PledgeCategoryResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.CategoryName, result.CategoryName);
        Assert.True(result.Active);
        Assert.Equal(DateTimeKind.Utc, result.CreatedAt.Kind);
        Assert.InRange(result.CreatedAt, beforeRequest, DateTime.UtcNow);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
        Assert.Equal($"/pledge-categories/{result.Id}", response.Headers.Location?.OriginalString);

        using var getResponse = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(result, await getResponse.Content.ReadFromJsonAsync<PledgeCategoryResponse>());
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        var storedCategory = await repository.GetByIdAsync(new PledgeCategoryId(result.Id), CancellationToken.None);
        Assert.NotNull(storedCategory);
        Assert.Equal(result.CategoryName, storedCategory.CategoryName);
        Assert.Equal(result.Active, storedCategory.Active);
        Assert.Equal(result.CreatedAt, storedCategory.CreatedAt);
        Assert.Equal(result.UpdatedAt, storedCategory.UpdatedAt);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"categoryName":null}""")]
    [InlineData("""{"categoryName":""}""")]
    [InlineData("""{"categoryName":"   "}""")]
    [InlineData("""{"categoryName":"\t\r\n"}""")]
    public async Task Post_with_invalid_name_returns_bad_request_without_adding_category(string json)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/pledge-categories", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Null(response.Headers.Location);
        Assert.Empty(await repository.GetAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Post_when_service_rejects_argument_returns_bad_request_problem_details()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IPledgeCategoryService>(_ => new RejectingPledgeCategoryService())));
        using var client = CreateClient(factory);
        var request = new CreatePledgeCategoryRequest { CategoryName = "Building Fund" };

        using var response = await client.PostAsJsonAsync("/pledge-categories", request);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category", problem.Title);
        Assert.Equal(RejectingPledgeCategoryService.RejectionMessage, problem.Detail);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Post_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeCategoryRepository>(new FailingPledgeCategoryRepository())));
        using var client = CreateClient(factory);
        var request = new CreatePledgeCategoryRequest { CategoryName = "Building Fund" };

        using var response = await client.PostAsJsonAsync("/pledge-categories", request);

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge category could not be created.", problem.Detail);
        Assert.Null(response.Headers.Location);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Fact]
    public async Task Get_returns_all_categories_as_response_array()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        var activeCategory = PledgeCategory.Create("Building Fund");
        var inactiveCategory = PledgeCategory.Create("Mission Fund");
        inactiveCategory.Update("Mission Fund", false);
        var categories = new[] { activeCategory, inactiveCategory };
        foreach (var category in categories)
        {
            await repository.AddAsync(category, CancellationToken.None);
        }

        using var response = await client.GetAsync("/pledge-categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var results = await response.Content.ReadFromJsonAsync<PledgeCategoryResponse[]>();
        Assert.NotNull(results);
        Assert.Equal(categories.Length, results.Length);
        foreach (var category in categories)
        {
            var result = Assert.Single(results, item => item.Id == category.Id.Value);
            Assert.Equal(category.CategoryName, result.CategoryName);
            Assert.Equal(category.Active, result.Active);
            Assert.Equal(category.CreatedAt, result.CreatedAt);
            Assert.Equal(category.UpdatedAt, result.UpdatedAt);
        }
    }

    [Fact]
    public async Task Get_with_no_categories_returns_empty_array()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/pledge-categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_when_service_rejects_argument_returns_bad_request_problem_details()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IPledgeCategoryService>(_ => new RejectingPledgeCategoryService())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/pledge-categories");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category query", problem.Title);
        Assert.Equal(RejectingPledgeCategoryService.RejectionMessage, problem.Detail);
    }

    [Fact]
    public async Task Get_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeCategoryRepository>(new FailingPledgeCategoryRepository())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/pledge-categories");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge categories could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Put_with_existing_id_updates_and_returns_pledge_category(bool active)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        var category = PledgeCategory.Create("Building Fund");
        var createdAt = category.CreatedAt;
        var originalUpdatedAt = category.UpdatedAt;
        await repository.AddAsync(category, CancellationToken.None);
        var request = new UpdatePledgeCategoryRequest { CategoryName = "Mission Fund", Active = active };

        using var response = await client.PutAsJsonAsync($"/pledge-categories/{category.Id.Value}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgeCategoryResponse>();
        Assert.NotNull(result);
        Assert.Equal(category.Id.Value, result.Id);
        Assert.Equal(request.CategoryName, result.CategoryName);
        Assert.Equal(active, result.Active);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.True(result.UpdatedAt >= originalUpdatedAt);

        using var getResponse = await client.GetAsync($"/pledge-categories/{category.Id.Value}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(result, await getResponse.Content.ReadFromJsonAsync<PledgeCategoryResponse>());
    }

    [Fact]
    public async Task Put_with_unknown_id_returns_not_found_without_adding_category()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        var id = Guid.NewGuid();
        var request = new UpdatePledgeCategoryRequest { CategoryName = "Mission Fund", Active = true };

        using var response = await client.PutAsJsonAsync($"/pledge-categories/{id}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        Assert.Null(await repository.GetByIdAsync(new PledgeCategoryId(id), CancellationToken.None));
    }

    [Fact]
    public async Task Put_with_non_guid_id_returns_not_found()
    {
        using var client = CreateClient(_factory);
        var request = new UpdatePledgeCategoryRequest { CategoryName = "Mission Fund", Active = true };

        using var response = await client.PutAsJsonAsync("/pledge-categories/abc", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);
        var request = new UpdatePledgeCategoryRequest { CategoryName = "Mission Fund", Active = true };

        using var response = await client.PutAsJsonAsync($"/pledge-categories/{Guid.Empty}", request);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category ID", problem.Title);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"active":true}""")]
    [InlineData("""{"categoryName":null,"active":true}""")]
    [InlineData("""{"categoryName":"","active":true}""")]
    [InlineData("""{"categoryName":"   ","active":true}""")]
    [InlineData("""{"categoryName":"Mission Fund"}""")]
    [InlineData("""{"categoryName":"Mission Fund","active":null}""")]
    public async Task Put_with_invalid_body_returns_bad_request_without_updating(string json)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        var category = PledgeCategory.Create("Building Fund");
        var originalUpdatedAt = category.UpdatedAt;
        await repository.AddAsync(category, CancellationToken.None);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"/pledge-categories/{category.Id.Value}", content);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        var storedCategory = await repository.GetByIdAsync(category.Id, CancellationToken.None);
        Assert.NotNull(storedCategory);
        Assert.Equal("Building Fund", storedCategory.CategoryName);
        Assert.True(storedCategory.Active);
        Assert.Equal(originalUpdatedAt, storedCategory.UpdatedAt);
    }

    [Fact]
    public async Task Put_when_service_rejects_argument_returns_bad_request_problem_details()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IPledgeCategoryService>(_ => new RejectingPledgeCategoryService())));
        using var client = CreateClient(factory);
        var request = new UpdatePledgeCategoryRequest { CategoryName = "Mission Fund", Active = true };

        using var response = await client.PutAsJsonAsync($"/pledge-categories/{Guid.NewGuid()}", request);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category update", problem.Title);
        Assert.Equal(RejectingPledgeCategoryService.RejectionMessage, problem.Detail);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Put_when_repository_fails_returns_sanitized_internal_server_error(bool failOnUpdate)
    {
        var category = failOnUpdate ? PledgeCategory.Create("Building Fund") : null;
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeCategoryRepository>(new FailingPledgeCategoryRepository(category))));
        using var client = CreateClient(factory);
        var id = category?.Id.Value ?? Guid.NewGuid();
        var request = new UpdatePledgeCategoryRequest { CategoryName = "Mission Fund", Active = false };

        using var response = await client.PutAsJsonAsync($"/pledge-categories/{id}", request);

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge category could not be updated.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Fact]
    public async Task Delete_with_existing_id_removes_category_and_returns_no_content()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        var category = PledgeCategory.Create("Building Fund");
        var otherCategory = PledgeCategory.Create("Mission Fund");
        await repository.AddAsync(category, CancellationToken.None);
        await repository.AddAsync(otherCategory, CancellationToken.None);

        using var response = await client.DeleteAsync($"/pledge-categories/{category.Id.Value}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Null(await repository.GetByIdAsync(category.Id, CancellationToken.None));
        Assert.NotNull(await repository.GetByIdAsync(otherCategory.Id, CancellationToken.None));
        using var getResponse = await client.GetAsync($"/pledge-categories/{category.Id.Value}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledge-categories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_twice_returns_not_found_on_second_request()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>();
        var category = PledgeCategory.Create("Building Fund");
        await repository.AddAsync(category, CancellationToken.None);

        using var firstResponse = await client.DeleteAsync($"/pledge-categories/{category.Id.Value}");
        using var secondResponse = await client.DeleteAsync($"/pledge-categories/{category.Id.Value}");

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondResponse.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    public async Task Delete_with_non_guid_id_returns_not_found(string id)
    {
        using var client = CreateClient(_factory);

        using var response = await client.DeleteAsync($"/pledge-categories/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.DeleteAsync($"/pledge-categories/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category ID", problem.Title);
        Assert.Equal("Pledge category ID cannot be empty.", problem.Detail);
    }

    [Fact]
    public async Task Delete_when_service_rejects_argument_returns_bad_request_problem_details()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IPledgeCategoryService>(_ => new RejectingPledgeCategoryService())));
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledge-categories/{Guid.NewGuid()}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category ID", problem.Title);
        Assert.Equal(RejectingPledgeCategoryService.RejectionMessage, problem.Detail);
    }

    [Fact]
    public async Task Delete_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeCategoryRepository>(new FailingPledgeCategoryRepository())));
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledge-categories/{Guid.NewGuid()}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge category could not be removed.", problem.Detail);
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
        private readonly PledgeCategory? _category;

        public FailingPledgeCategoryRepository(PledgeCategory? category = null)
        {
            _category = category;
        }

        public Task<IReadOnlyList<PledgeCategory>> GetAllAsync(CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<PledgeCategory?> GetByIdAsync(PledgeCategoryId id, CancellationToken cancellationToken)
            => _category is null
                ? throw new InvalidOperationException("Internal repository failure")
                : Task.FromResult<PledgeCategory?>(_category);

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
