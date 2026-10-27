using System.Net;
using System.Net.Http.Json;
using ICFAccounting.Api.Models;
using ICFAccounting.Api.Models.PledgerModels;
using ICFAccounting.Application.Services;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Repositories;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using static ICFAccounting.IntegrationTests.Api.ApiTestHelpers;

namespace ICFAccounting.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class PledgerEndpointsTests : SqlServerApiTestBase
{
    public PledgerEndpointsTests(SqlServerModelFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Post_with_required_fields_returns_created_details_and_location()
    {
        using var client = CreateClient(_factory, allowAutoRedirect: false);
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
        using var client = CreateClient(_factory, allowAutoRedirect: false);
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var originalCount = (await repository.GetAllAsync()).TotalCount;
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/pledgers", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.Null(response.Headers.Location);
        Assert.Equal(originalCount, (await repository.GetAllAsync()).TotalCount);
    }

    [Fact]
    public async Task Post_when_service_rejects_pledger_returns_bad_request_problem_details()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IPledgerService>(_ => new RejectingPledgerService())));
        using var client = CreateClient(factory, allowAutoRedirect: false);

        using var response = await client.PostAsJsonAsync(
            "/pledgers", new CreatePledgerRequest { PledgerName = "Community Fund" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Invalid pledger", problem.Title);
        Assert.Equal(RejectingPledgerService.RejectionMessage, problem.Detail);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Post_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgerRepository>(new FailingPledgerRepository())));
        using var client = CreateClient(factory, allowAutoRedirect: false);

        using var response = await client.PostAsJsonAsync(
            "/pledgers", new CreatePledgerRequest { PledgerName = "Community Fund" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledger could not be created.", problem.Detail);
        Assert.Null(response.Headers.Location);
        Assert.DoesNotContain("Internal repository failure", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_without_query_returns_default_sorted_page_and_response_details()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
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
        using var client = CreateClient(factory);
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
        using var client = CreateClient(_factory);

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
        using var client = CreateClient(factory);

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
        using var client = CreateClient(factory);

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

    [Fact]
    public async Task Get_by_id_with_existing_id_returns_pledger_details()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var pledger = Pledger.Create("Community Fund");
        await repository.AddAsync(pledger, CancellationToken.None);

        using var response = await client.GetAsync($"/pledgers/{pledger.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgerResponse>();
        Assert.NotNull(result);
        Assert.Equal(pledger.Id.Value, result.Id);
        Assert.Equal(pledger.PledgerName, result.PledgerName);
        Assert.Equal(pledger.Active, result.Active);
        Assert.Equal(pledger.CreatedAt, result.CreatedAt);
        Assert.Equal(pledger.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task Get_by_id_returns_pledger_created_via_post_location()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory, allowAutoRedirect: false);
        using var postResponse = await client.PostAsJsonAsync(
            "/pledgers", new CreatePledgerRequest { PledgerName = "Community Fund" });
        var created = await postResponse.Content.ReadFromJsonAsync<PledgerResponse>();

        using var response = await client.GetAsync(postResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created, await response.Content.ReadFromJsonAsync<PledgerResponse>());
    }

    [Fact]
    public async Task Get_by_id_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledgers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    public async Task Get_by_id_with_non_guid_id_returns_not_found(string id)
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledgers/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledgers/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }

    [Fact]
    public async Task Get_by_id_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgerRepository>(new FailingPledgerRepository())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledgers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledger could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Put_with_existing_id_updates_and_returns_pledger()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var pledger = Pledger.Create("Community Fund");
        var createdAt = pledger.CreatedAt;
        await repository.AddAsync(pledger, CancellationToken.None);
        var request = new UpdatePledgerRequest { PledgerName = "Mission Fund", Active = false };

        using var response = await client.PutAsJsonAsync($"/pledgers/{pledger.Id.Value}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgerResponse>();
        Assert.NotNull(result);
        Assert.Equal(pledger.Id.Value, result.Id);
        Assert.Equal("Mission Fund", result.PledgerName);
        Assert.False(result.Active);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.True(result.UpdatedAt >= createdAt);

        var storedPledger = await repository.GetByIdAsync(pledger.Id, CancellationToken.None);
        Assert.NotNull(storedPledger);
        Assert.Equal("Mission Fund", storedPledger.PledgerName);
        Assert.False(storedPledger.Active);
        Assert.Equal(result.UpdatedAt, storedPledger.UpdatedAt);
    }

    [Fact]
    public async Task Put_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        var request = new UpdatePledgerRequest { PledgerName = "Mission Fund", Active = true };

        using var response = await client.PutAsJsonAsync($"/pledgers/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_non_guid_id_returns_not_found()
    {
        using var client = CreateClient(_factory);
        var request = new UpdatePledgerRequest { PledgerName = "Mission Fund", Active = true };

        using var response = await client.PutAsJsonAsync("/pledgers/abc", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);
        var request = new UpdatePledgerRequest { PledgerName = "Mission Fund", Active = true };

        using var response = await client.PutAsJsonAsync($"/pledgers/{Guid.Empty}", request);

        await AssertBadRequestProblemAsync(response);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"active":true}""")]
    [InlineData("""{"pledgerName":null,"active":true}""")]
    [InlineData("""{"pledgerName":"","active":true}""")]
    [InlineData("""{"pledgerName":"   ","active":true}""")]
    [InlineData("""{"pledgerName":"Mission Fund"}""")]
    [InlineData("""{"pledgerName":"Mission Fund","active":null}""")]
    public async Task Put_with_invalid_body_returns_bad_request_without_updating(string json)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var pledger = Pledger.Create("Community Fund");
        var originalUpdatedAt = pledger.UpdatedAt;
        await repository.AddAsync(pledger, CancellationToken.None);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"/pledgers/{pledger.Id.Value}", content);

        await AssertBadRequestProblemAsync(response);
        var storedPledger = await repository.GetByIdAsync(pledger.Id, CancellationToken.None);
        Assert.NotNull(storedPledger);
        Assert.Equal("Community Fund", storedPledger.PledgerName);
        Assert.True(storedPledger.Active);
        Assert.Equal(originalUpdatedAt, storedPledger.UpdatedAt);
    }

    [Fact]
    public async Task Put_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgerRepository>(new FailingPledgerRepository())));
        using var client = CreateClient(factory);
        var request = new UpdatePledgerRequest { PledgerName = "Mission Fund", Active = true };

        using var response = await client.PutAsJsonAsync($"/pledgers/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledger could not be updated.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Delete_with_existing_id_removes_pledger_and_returns_no_content()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var pledger = Pledger.Create("Community Fund");
        await repository.AddAsync(pledger, CancellationToken.None);

        using var response = await client.DeleteAsync($"/pledgers/{pledger.Id.Value}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Null(await repository.GetByIdAsync(pledger.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_then_get_by_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory, allowAutoRedirect: false);
        using var createResponse = await client.PostAsJsonAsync(
            "/pledgers", new CreatePledgerRequest { PledgerName = "Community Fund" });
        var location = createResponse.Headers.Location;
        Assert.NotNull(location);

        using var deleteResponse = await client.DeleteAsync(location);
        using var getResponse = await client.GetAsync(location);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledgers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_twice_returns_not_found_on_second_request()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgerRepository>();
        var pledger = Pledger.Create("Community Fund");
        await repository.AddAsync(pledger, CancellationToken.None);

        using var firstResponse = await client.DeleteAsync($"/pledgers/{pledger.Id.Value}");
        using var secondResponse = await client.DeleteAsync($"/pledgers/{pledger.Id.Value}");

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondResponse.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    public async Task Delete_with_non_guid_id_returns_not_found(string id)
    {
        using var client = CreateClient(_factory);

        using var response = await client.DeleteAsync($"/pledgers/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.DeleteAsync($"/pledgers/{Guid.Empty}");

        await AssertBadRequestProblemAsync(response);
    }

    [Fact]
    public async Task Delete_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgerRepository>(new FailingPledgerRepository())));
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledgers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledger could not be removed.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", await response.Content.ReadAsStringAsync());
    }

    public static TheoryData<string, string, string> ServiceRejectionRequests => new()
    {
        { "GET", "/pledgers", "Invalid pledger query" },
        { "GET", $"/pledgers/{Guid.NewGuid()}", "Invalid pledger ID" },
        { "PUT", $"/pledgers/{Guid.NewGuid()}", "Invalid pledger update" },
        { "DELETE", $"/pledgers/{Guid.NewGuid()}", "Invalid pledger ID" }
    };

    [Theory]
    [MemberData(nameof(ServiceRejectionRequests))]
    public async Task Request_when_service_rejects_argument_returns_bad_request_problem_details(
        string method, string path, string expectedTitle)
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddScoped<IPledgerService>(_ => new RejectingPledgerService())));
        using var client = CreateClient(factory);
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(
                new UpdatePledgerRequest { PledgerName = "Mission Fund", Active = true });
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.Equal(expectedTitle, problem.Title);
        Assert.Equal(RejectingPledgerService.RejectionMessage, problem.Detail);
    }

    private static async Task AssertBadRequestProblemAsync(HttpResponseMessage response)
    {
        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }

    private sealed class FailingPledgerRepository : IPledgerRepository
    {
        public Task<PagedPledgersResult> GetAllAsync(
            PledgerQuery? query = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Internal repository failure");

        public Task AddAsync(Pledger pledger, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task UpdateAsync(Pledger pledger, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task RemoveAsync(PledgerId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");
    }

    private sealed class RejectingPledgerService : IPledgerService
    {
        public const string RejectionMessage = "PledgerName exceeds the maximum length.";

        public Task AddAsync(Pledger pledger, CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage, nameof(pledger));

        public Task<Pledger?> GetByIdAsync(PledgerId id, CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage, nameof(id));

        public Task<PagedPledgersResult> GetAllAsync(
            PledgerQuery? query = null, CancellationToken cancellationToken = default)
            => throw new ArgumentException(RejectionMessage, nameof(query));

        public Task<Pledger?> UpdateAsync(
            PledgerId id, string pledgerName, bool active, CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage, nameof(pledgerName));

        public Task RemoveAsync(PledgerId id, CancellationToken cancellationToken)
            => throw new ArgumentException(RejectionMessage, nameof(id));
    }
}
