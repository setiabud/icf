using System.Net;
using System.Net.Http.Json;
using ICFAccounting.Api;
using ICFAccounting.Api.Models.PledgeModels;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.PledgeCategories;
using ICFAccounting.Domain.Pledgers;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using static ICFAccounting.IntegrationTests.Api.ApiTestHelpers;

namespace ICFAccounting.IntegrationTests.Api;

public sealed class PledgeEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string ValidPledgerId = "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a01";
    private const string ValidCategoryId = "6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a02";
    private const string EmptyGuid = "00000000-0000-0000-0000-000000000000";

    private readonly WebApplicationFactory<Program> _factory;

    public PledgeEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_with_valid_request_returns_created_details_and_location()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory, allowAutoRedirect: false);
        var request = new CreatePledgeRequest
        {
            Year = 2026,
            Amount = 125.50m,
            PledgerId = Guid.NewGuid(),
            PledgeCategoryId = Guid.NewGuid(),
            Comment = "  Annual pledge  "
        };
        var beforeRequest = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync("/pledges", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgeResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Year, result.Year);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.PledgerId, result.PledgerId);
        Assert.Equal(request.PledgeCategoryId, result.PledgeCategoryId);
        Assert.Equal("Annual pledge", result.Comment);
        Assert.InRange(result.CreatedAt, beforeRequest, DateTime.UtcNow);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
        Assert.Equal($"/pledges/{result.Id}", response.Headers.Location?.OriginalString);

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var storedPledge = await repository.GetByIdAsync(new PledgeId(result.Id), CancellationToken.None);
        Assert.NotNull(storedPledge);
        Assert.Equal(request.Amount, storedPledge.Amount);
        Assert.Equal(result.CreatedAt, storedPledge.CreatedAt);
    }

    [Fact]
    public async Task Post_then_get_by_location_returns_created_pledge()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory, allowAutoRedirect: false);

        using var postResponse = await client.PostAsJsonAsync("/pledges", CreateValidCreateRequest());
        using var getResponse = await client.GetAsync(postResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<PledgeResponse>();
        var retrieved = await getResponse.Content.ReadFromJsonAsync<PledgeResponse>();
        Assert.Equal(created, retrieved);
    }

    [Theory]
    [InlineData(2019, "100", ValidPledgerId, ValidCategoryId, "Year must be between 2020 and 2099.")]
    [InlineData(2100, "100", ValidPledgerId, ValidCategoryId, "Year must be between 2020 and 2099.")]
    [InlineData(2026, "0", ValidPledgerId, ValidCategoryId, "Amount must be greater than zero.")]
    [InlineData(2026, "-5", ValidPledgerId, ValidCategoryId, "Amount must be greater than zero.")]
    [InlineData(2026, "1.001", ValidPledgerId, ValidCategoryId, "Amount must have at most two decimal places.")]
    [InlineData(2026, "100", EmptyGuid, ValidCategoryId, "Pledger ID cannot be empty.")]
    [InlineData(2026, "100", ValidPledgerId, EmptyGuid, "Pledge category ID cannot be empty.")]
    public async Task Post_with_invalid_values_returns_bad_request_without_creating(
        int year, string amount, string pledgerId, string pledgeCategoryId, string expectedDetail)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory, allowAutoRedirect: false);
        var json = $$"""{"year":{{year}},"amount":{{amount}},"pledgerId":"{{pledgerId}}","pledgeCategoryId":"{{pledgeCategoryId}}"}""";
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/pledges", content);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge", problem.Title);
        Assert.Equal(expectedDetail, problem.Detail);
        Assert.Null(response.Headers.Location);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var stored = await repository.GetByCategoryAsync(
            new PledgeCategoryId(Guid.Parse(ValidCategoryId)), cancellationToken: CancellationToken.None);
        Assert.Equal(0, stored.TotalCount);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"amount":100,"pledgerId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a01","pledgeCategoryId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a02"}""")]
    [InlineData("""{"year":2026,"pledgerId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a01","pledgeCategoryId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a02"}""")]
    [InlineData("""{"year":2026,"amount":100,"pledgeCategoryId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a02"}""")]
    [InlineData("""{"year":2026,"amount":100,"pledgerId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a01"}""")]
    [InlineData("not json")]
    public async Task Post_with_missing_fields_or_malformed_body_returns_bad_request(string json)
    {
        using var client = CreateClient(_factory, allowAutoRedirect: false);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/pledges", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Post_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeRepository>(new FailingPledgeRepository())));
        using var client = CreateClient(factory, allowAutoRedirect: false);

        using var response = await client.PostAsJsonAsync("/pledges", CreateValidCreateRequest());

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge could not be created.", problem.Detail);
        Assert.Null(response.Headers.Location);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Fact]
    public async Task Get_by_id_with_existing_id_returns_pledge_details()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var pledge = Pledge.Create(2026, 125.50m, PledgerId.New(), PledgeCategoryId.New(), "Annual pledge");
        await repository.AddAsync(pledge, CancellationToken.None);

        using var response = await client.GetAsync($"/pledges/{pledge.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgeResponse>();
        Assert.NotNull(result);
        Assert.Equal(pledge.Id.Value, result.Id);
        Assert.Equal(pledge.Year, result.Year);
        Assert.Equal(pledge.Amount, result.Amount);
        Assert.Equal(pledge.PledgerId.Value, result.PledgerId);
        Assert.Equal(pledge.PledgeCategoryId.Value, result.PledgeCategoryId);
        Assert.Equal(pledge.Comment, result.Comment);
        Assert.Equal(pledge.CreatedAt, result.CreatedAt);
        Assert.Equal(pledge.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task Get_by_id_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledges/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    public async Task Get_by_id_with_non_guid_id_returns_not_found(string id)
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledges/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledges/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge ID", problem.Title);
        Assert.Equal("Pledge ID cannot be empty.", problem.Detail);
    }

    [Fact]
    public async Task Get_by_id_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeRepository>(new FailingPledgeRepository())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledges/{Guid.NewGuid()}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Theory]
    [InlineData("Ascending", 1, 2023)]
    [InlineData("Ascending", 2, 2024)]
    [InlineData("Descending", 1, 2025)]
    public async Task Get_by_pledger_returns_paginated_sorted_pledges(
        string sortOrder, int page, int expectedYear)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var pledgerId = PledgerId.New();
        var categoryId = PledgeCategoryId.New();
        foreach (var year in new[] { 2025, 2023, 2024 })
        {
            await repository.AddAsync(
                Pledge.Create(year, 100m, pledgerId, categoryId), CancellationToken.None);
        }

        await repository.AddAsync(
            Pledge.Create(2026, 100m, PledgerId.New(), categoryId), CancellationToken.None);

        using var response = await client.GetAsync(
            $"/pledges/pledger/{pledgerId.Value}?page={page}&pageSize=1&sortBy=Year&sortOrder={sortOrder}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePageResponse>();
        Assert.NotNull(result);
        Assert.Equal(page, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(expectedYear, Assert.Single(result.Items).Year);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?sortBy=0")]
    [InlineData("?sortOrder=asc")]
    [InlineData("?sortOrder=2")]
    public async Task Get_by_pledger_with_invalid_query_returns_bad_request(string query)
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledges/pledger/{PledgerId.New().Value}{query}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge query", problem.Title);
    }

    [Fact]
    public async Task Get_by_pledger_with_empty_id_returns_bad_request()
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledges/pledger/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledger ID", problem.Title);
    }

    [Fact]
    public async Task Get_by_pledger_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeRepository>(new FailingPledgeRepository())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledges/pledger/{PledgerId.New().Value}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("Pledges could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
    }

    [Theory]
    [InlineData("Ascending", "50,100,150")]
    [InlineData("Descending", "150,100,50")]
    public async Task Get_by_pledger_sorts_by_amount(string sortOrder, string expectedAmounts)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var pledgerId = PledgerId.New();
        var categoryId = PledgeCategoryId.New();
        foreach (var (year, amount) in new[] { (2025, 100m), (2023, 150m), (2024, 50m) })
        {
            await repository.AddAsync(
                Pledge.Create(year, amount, pledgerId, categoryId), CancellationToken.None);
        }

        using var response = await client.GetAsync(
            $"/pledges/pledger/{pledgerId.Value}?sortBy=Amount&sortOrder={sortOrder}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePageResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedAmounts, string.Join(",", result.Items.Select(pledge => pledge.Amount)));
    }

    [Theory]
    [InlineData("Ascending", 1, 2023)]
    [InlineData("Ascending", 2, 2024)]
    [InlineData("Descending", 1, 2025)]
    public async Task Get_by_category_returns_paginated_sorted_pledges(
        string sortOrder, int page, int expectedYear)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var categoryId = PledgeCategoryId.New();
        var pledgerId = PledgerId.New();
        foreach (var year in new[] { 2025, 2023, 2024 })
        {
            await repository.AddAsync(
                Pledge.Create(year, 100m, pledgerId, categoryId), CancellationToken.None);
        }

        await repository.AddAsync(
            Pledge.Create(2026, 100m, pledgerId, PledgeCategoryId.New()), CancellationToken.None);

        using var response = await client.GetAsync(
            $"/pledges/pledge-category/{categoryId.Value}?page={page}&pageSize=1&sortBy=Year&sortOrder={sortOrder}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePageResponse>();
        Assert.NotNull(result);
        Assert.Equal(page, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(expectedYear, Assert.Single(result.Items).Year);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?sortOrder=desc")]
    public async Task Get_by_category_with_invalid_query_returns_bad_request(string query)
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync(
            $"/pledges/pledge-category/{PledgeCategoryId.New().Value}{query}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge query", problem.Title);
    }

    [Fact]
    public async Task Get_by_category_with_empty_id_returns_bad_request()
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledges/pledge-category/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge category ID", problem.Title);
        Assert.Equal("Pledge category ID cannot be empty.", problem.Detail);
    }

    [Fact]
    public async Task Get_by_category_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeRepository>(new FailingPledgeRepository())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(
            $"/pledges/pledge-category/{PledgeCategoryId.New().Value}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("Pledges could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
    }

    [Theory]
    [InlineData("Ascending", "50,100,150")]
    [InlineData("Descending", "150,100,50")]
    public async Task Get_by_category_sorts_by_amount(string sortOrder, string expectedAmounts)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var categoryId = PledgeCategoryId.New();
        var pledgerId = PledgerId.New();
        foreach (var (year, amount) in new[] { (2025, 100m), (2023, 150m), (2024, 50m) })
        {
            await repository.AddAsync(
                Pledge.Create(year, amount, pledgerId, categoryId), CancellationToken.None);
        }

        using var response = await client.GetAsync(
            $"/pledges/pledge-category/{categoryId.Value}?sortBy=Amount&sortOrder={sortOrder}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePageResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedAmounts, string.Join(",", result.Items.Select(pledge => pledge.Amount)));
    }

    [Fact]
    public async Task Put_with_existing_id_updates_and_returns_pledge()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var pledge = Pledge.Create(2025, 100m, PledgerId.New(), PledgeCategoryId.New(), "Original");
        var createdAt = pledge.CreatedAt;
        await repository.AddAsync(pledge, CancellationToken.None);
        var updatedPledgerId = PledgerId.New();
        var updatedCategoryId = PledgeCategoryId.New();
        var request = new UpdatePledgeRequest
        {
            Year = 2026,
            Amount = 125.50m,
            PledgerId = updatedPledgerId.Value,
            PledgeCategoryId = updatedCategoryId.Value,
            Comment = "Updated pledge"
        };

        using var response = await client.PutAsJsonAsync($"/pledges/{pledge.Id.Value}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgeResponse>();
        Assert.NotNull(result);
        Assert.Equal(pledge.Id.Value, result.Id);
        Assert.Equal(request.Year, result.Year);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.PledgerId, result.PledgerId);
        Assert.Equal(request.PledgeCategoryId, result.PledgeCategoryId);
        Assert.Equal(request.Comment, result.Comment);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.True(result.UpdatedAt >= createdAt);

        var storedPledge = await repository.GetByIdAsync(pledge.Id, CancellationToken.None);
        Assert.NotNull(storedPledge);
        Assert.Equal(result, new PledgeResponse
        {
            Id = storedPledge.Id.Value,
            Year = storedPledge.Year,
            Amount = storedPledge.Amount,
            PledgerId = storedPledge.PledgerId.Value,
            PledgeCategoryId = storedPledge.PledgeCategoryId.Value,
            Comment = storedPledge.Comment,
            CreatedAt = storedPledge.CreatedAt,
            UpdatedAt = storedPledge.UpdatedAt
        });
    }

    [Fact]
    public async Task Put_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.PutAsJsonAsync(
            $"/pledges/{Guid.NewGuid()}", CreateValidUpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_non_guid_id_returns_not_found()
    {
        using var client = CreateClient(_factory);

        using var response = await client.PutAsJsonAsync("/pledges/not-a-guid", CreateValidUpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.PutAsJsonAsync(
            $"/pledges/{Guid.Empty}", CreateValidUpdateRequest());

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge ID", problem.Title);
        Assert.Equal("Pledge ID cannot be empty.", problem.Detail);
    }

    [Theory]
    [InlineData("{}")] 
    [InlineData("""{"year":2026,"amount":100,"pledgerId":"00000000-0000-0000-0000-000000000001"}""")]
    [InlineData("""{"year":2026,"pledgerId":"00000000-0000-0000-0000-000000000001","pledgeCategoryId":"00000000-0000-0000-0000-000000000002"}""")]
    [InlineData("""{"amount":100,"pledgerId":"00000000-0000-0000-0000-000000000001","pledgeCategoryId":"00000000-0000-0000-0000-000000000002"}""")]
    [InlineData("""{"year":2026,"amount":100,"pledgeCategoryId":"00000000-0000-0000-0000-000000000002"}""")]
    [InlineData("""{"year":2026,"amount":100,"pledgerId":"00000000-0000-0000-0000-000000000001","pledgeCategoryId":"00000000-0000-0000-0000-000000000000"}""")]
    public async Task Put_with_invalid_body_returns_bad_request_without_updating(string json)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var pledge = Pledge.Create(2025, 100m, PledgerId.New(), PledgeCategoryId.New(), "Original");
        var originalUpdatedAt = pledge.UpdatedAt;
        await repository.AddAsync(pledge, CancellationToken.None);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"/pledges/{pledge.Id.Value}", content);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge update", problem.Title);
        var storedPledge = await repository.GetByIdAsync(pledge.Id, CancellationToken.None);
        Assert.NotNull(storedPledge);
        Assert.Equal(2025, storedPledge.Year);
        Assert.Equal(100m, storedPledge.Amount);
        Assert.Equal("Original", storedPledge.Comment);
        Assert.Equal(originalUpdatedAt, storedPledge.UpdatedAt);
    }

    [Fact]
    public async Task Put_with_invalid_year_returns_bad_request_without_updating()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var pledge = Pledge.Create(2025, 100m, PledgerId.New(), PledgeCategoryId.New(), "Original");
        var originalUpdatedAt = pledge.UpdatedAt;
        await repository.AddAsync(pledge, CancellationToken.None);
        var request = CreateValidUpdateRequest() with { Year = 2019 };

        using var response = await client.PutAsJsonAsync($"/pledges/{pledge.Id.Value}", request);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge update", problem.Title);
        var storedPledge = await repository.GetByIdAsync(pledge.Id, CancellationToken.None);
        Assert.NotNull(storedPledge);
        Assert.Equal(2025, storedPledge.Year);
        Assert.Equal(originalUpdatedAt, storedPledge.UpdatedAt);
    }

    [Fact]
    public async Task Put_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeRepository>(new FailingPledgeRepository())));
        using var client = CreateClient(factory);

        using var response = await client.PutAsJsonAsync(
            $"/pledges/{Guid.NewGuid()}", CreateValidUpdateRequest());

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge could not be updated.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Fact]
    public async Task Delete_with_existing_id_removes_pledge_and_returns_no_content()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgeRepository>();
        var pledge = Pledge.Create(2026, 100m, PledgerId.New(), PledgeCategoryId.New());
        await repository.AddAsync(pledge, CancellationToken.None);

        using var response = await client.DeleteAsync($"/pledges/{pledge.Id.Value}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Null(await repository.GetByIdAsync(pledge.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledges/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_with_non_guid_id_returns_not_found()
    {
        using var client = CreateClient(_factory);

        using var response = await client.DeleteAsync("/pledges/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.DeleteAsync($"/pledges/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge ID", problem.Title);
        Assert.Equal("Pledge ID cannot be empty.", problem.Detail);
    }

    [Fact]
    public async Task Delete_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeRepository>(new FailingPledgeRepository())));
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledges/{Guid.NewGuid()}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge could not be removed.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    private static CreatePledgeRequest CreateValidCreateRequest()
    {
        return new CreatePledgeRequest
        {
            Year = 2026,
            Amount = 100m,
            PledgerId = PledgerId.New().Value,
            PledgeCategoryId = PledgeCategoryId.New().Value
        };
    }

    private static UpdatePledgeRequest CreateValidUpdateRequest()
    {
        return new UpdatePledgeRequest
        {
            Year = 2026,
            Amount = 100m,
            PledgerId = PledgerId.New().Value,
            PledgeCategoryId = PledgeCategoryId.New().Value
        };
    }

    private WebApplicationFactory<Program> CreateIsolatedFactory()
    {
        return _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgeRepository, InMemoryPledgeRepository>()));
    }

    private sealed class FailingPledgeRepository : IPledgeRepository
    {
        public Task AddAsync(Pledge pledge, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<Pledge?> GetByIdAsync(PledgeId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<PagedPledgesResult> GetByPledgerAsync(
            PledgerId pledgerId, PledgeQuery? query = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<PagedPledgesResult> GetByCategoryAsync(
            PledgeCategoryId pledgeCategoryId,
            PledgeQuery? query = null,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Internal repository failure");

        public Task UpdateAsync(Pledge pledge, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task RemoveAsync(PledgeId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");
    }
}