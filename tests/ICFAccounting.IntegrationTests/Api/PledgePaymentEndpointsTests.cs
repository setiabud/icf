using System.Net;
using System.Net.Http.Json;
using ICFAccounting.Api;
using ICFAccounting.Api.Models.PledgePaymentModels;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.PledgePayments;
using ICFAccounting.Domain.Pledges;
using ICFAccounting.Domain.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using static ICFAccounting.IntegrationTests.Api.ApiTestHelpers;

namespace ICFAccounting.IntegrationTests.Api;

public sealed class PledgePaymentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PledgePaymentEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_with_valid_request_returns_created_details_and_location()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory, allowAutoRedirect: false);
        var request = new CreatePledgePaymentRequest
        {
            PledgeId = Guid.NewGuid(),
            Amount = 125.50m,
            PaymentDate = new DateTime(2026, 1, 20),
            Comment = "  Monthly payment  "
        };
        var beforeRequest = DateTime.UtcNow;

        using var response = await client.PostAsJsonAsync("/pledge-payments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePaymentResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.PledgeId, result.PledgeId);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.PaymentDate, result.PaymentDate);
        Assert.Equal("Monthly payment", result.Comment);
        Assert.True(result.Active);
        Assert.InRange(result.CreatedAt, beforeRequest, DateTime.UtcNow);
        Assert.Equal(result.CreatedAt, result.UpdatedAt);
        Assert.Equal($"/pledge-payments/{result.Id}", response.Headers.Location?.OriginalString);

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgePaymentRepository>();
        var storedPayment = await repository.GetByIdAsync(new PledgePaymentId(result.Id), CancellationToken.None);
        Assert.NotNull(storedPayment);
        Assert.Equal(request.Amount, storedPayment.Amount);
        Assert.Equal(result.CreatedAt, storedPayment.CreatedAt);
    }

    [Fact]
    public async Task Post_then_get_by_location_returns_created_payment()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory, allowAutoRedirect: false);

        using var postResponse = await client.PostAsJsonAsync("/pledge-payments", CreateValidCreateRequest());
        using var getResponse = await client.GetAsync(postResponse.Headers.Location);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<PledgePaymentResponse>();
        var retrieved = await getResponse.Content.ReadFromJsonAsync<PledgePaymentResponse>();
        Assert.Equal(created, retrieved);
    }

    [Theory]
    [InlineData("""{"pledgeId":"00000000-0000-0000-0000-000000000000","amount":100,"paymentDate":"2026-01-20T00:00:00"}""", "Pledge ID cannot be empty.")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","amount":0,"paymentDate":"2026-01-20T00:00:00"}""", "Amount must be greater than zero.")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","amount":-5,"paymentDate":"2026-01-20T00:00:00"}""", "Amount must be greater than zero.")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","amount":1.001,"paymentDate":"2026-01-20T00:00:00"}""", "Amount must have at most two decimal places.")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","amount":100,"paymentDate":"0001-01-01T00:00:00"}""", "Payment date cannot be empty.")]
    public async Task Post_with_invalid_values_returns_bad_request_without_creating(string json, string expectedDetail)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory, allowAutoRedirect: false);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/pledge-payments", content);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge payment", problem.Title);
        Assert.Equal(expectedDetail, problem.Detail);
        Assert.Null(response.Headers.Location);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgePaymentRepository>();
        var stored = await repository.GetByPledgeAsync(
            new PledgeId(Guid.Parse("6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00")), cancellationToken: CancellationToken.None);
        Assert.Equal(0, stored.TotalCount);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"amount":100,"paymentDate":"2026-01-20T00:00:00"}""")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","paymentDate":"2026-01-20T00:00:00"}""")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","amount":100}""")]
    [InlineData("not json")]
    public async Task Post_with_missing_fields_or_malformed_body_returns_bad_request(string json)
    {
        using var client = CreateClient(_factory, allowAutoRedirect: false);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/pledge-payments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Post_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgePaymentRepository>(new FailingPledgePaymentRepository())));
        using var client = CreateClient(factory, allowAutoRedirect: false);

        using var response = await client.PostAsJsonAsync("/pledge-payments", CreateValidCreateRequest());

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge payment could not be created.", problem.Detail);
        Assert.Null(response.Headers.Location);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Fact]
    public async Task Get_by_id_with_existing_id_returns_pledge_payment_details()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgePaymentRepository>();
        var payment = PledgePayment.Create(PledgeId.New(), 125.50m, new DateTime(2026, 1, 10), "Monthly payment");
        await repository.AddAsync(payment, CancellationToken.None);

        using var response = await client.GetAsync($"/pledge-payments/{payment.Id.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePaymentResponse>();
        Assert.NotNull(result);
        Assert.Equal(payment.Id.Value, result.Id);
        Assert.Equal(payment.PledgeId.Value, result.PledgeId);
        Assert.Equal(payment.Amount, result.Amount);
        Assert.Equal(payment.PaymentDate, result.PaymentDate);
        Assert.Equal(payment.Comment, result.Comment);
        Assert.Equal(payment.Active, result.Active);
        Assert.Equal(payment.CreatedAt, result.CreatedAt);
        Assert.Equal(payment.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public async Task Get_by_id_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledge-payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123")]
    public async Task Get_by_id_with_non_guid_id_returns_not_found(string id)
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledge-payments/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_id_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledge-payments/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge payment ID", problem.Title);
        Assert.Equal("Pledge payment ID cannot be empty.", problem.Detail);
    }

    [Fact]
    public async Task Get_by_id_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgePaymentRepository>(new FailingPledgePaymentRepository())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledge-payments/{Guid.NewGuid()}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge payment could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Theory]
    [InlineData("Ascending", 1, 150)]
    [InlineData("Ascending", 2, 50)]
    [InlineData("Descending", 1, 100)]
    public async Task Get_by_pledge_returns_paginated_payments_sorted_by_payment_date(
        string sortOrder, int page, decimal expectedAmount)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        var pledgeId = await SeedPaymentsAsync(factory);

        using var response = await client.GetAsync(
            $"/pledge-payments/pledge/{pledgeId.Value}?page={page}&pageSize=1&sortBy=PaymentDate&sortOrder={sortOrder}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePaymentPageResponse>();
        Assert.NotNull(result);
        Assert.Equal(page, result.Page);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        var payment = Assert.Single(result.Items);
        Assert.Equal(expectedAmount, payment.Amount);
        Assert.Equal(pledgeId.Value, payment.PledgeId);
    }

    [Theory]
    [InlineData("Ascending", "50,100,150")]
    [InlineData("descending", "150,100,50")]
    public async Task Get_by_pledge_sorts_by_amount(string sortOrder, string expectedAmounts)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        var pledgeId = await SeedPaymentsAsync(factory);

        using var response = await client.GetAsync(
            $"/pledge-payments/pledge/{pledgeId.Value}?sortBy=amount&sortOrder={sortOrder}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePaymentPageResponse>();
        Assert.NotNull(result);
        Assert.Equal(expectedAmounts, string.Join(",", result.Items.Select(payment => payment.Amount)));
    }

    [Fact]
    public async Task Get_by_pledge_without_query_uses_defaults()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        var pledgeId = await SeedPaymentsAsync(factory);

        using var response = await client.GetAsync($"/pledge-payments/pledge/{pledgeId.Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePaymentPageResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal("100,50,150", string.Join(",", result.Items.Select(payment => payment.Amount)));
    }

    [Fact]
    public async Task Get_by_pledge_with_no_payments_returns_empty_page()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledge-payments/pledge/{PledgeId.New().Value}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePaymentPageResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?sortBy=Year")]
    [InlineData("?sortBy=0")]
    [InlineData("?sortOrder=asc")]
    [InlineData("?sortOrder=2")]
    public async Task Get_by_pledge_with_invalid_query_returns_bad_request(string query)
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledge-payments/pledge/{PledgeId.New().Value}{query}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge payment query", problem.Title);
    }

    [Fact]
    public async Task Get_by_pledge_with_empty_id_returns_bad_request()
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync($"/pledge-payments/pledge/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge ID", problem.Title);
        Assert.Equal("Pledge ID cannot be empty.", problem.Detail);
    }

    [Fact]
    public async Task Get_by_pledge_with_non_guid_id_returns_not_found()
    {
        using var client = CreateClient(_factory);

        using var response = await client.GetAsync("/pledge-payments/pledge/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_by_pledge_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgePaymentRepository>(new FailingPledgePaymentRepository())));
        using var client = CreateClient(factory);

        using var response = await client.GetAsync($"/pledge-payments/pledge/{PledgeId.New().Value}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge payments could not be retrieved.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Fact]
    public async Task Put_with_existing_id_updates_and_returns_pledge_payment()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgePaymentRepository>();
        var payment = PledgePayment.Create(PledgeId.New(), 100m, new DateTime(2026, 1, 10), "Original");
        var updatedPledgeId = PledgeId.New();
        var createdAt = payment.CreatedAt;
        await repository.AddAsync(payment, CancellationToken.None);
        var request = new UpdatePledgePaymentRequest
        {
            PledgeId = updatedPledgeId.Value,
            Amount = 125.50m,
            PaymentDate = new DateTime(2026, 1, 20),
            Comment = "Updated payment",
            Active = false
        };

        using var response = await client.PutAsJsonAsync($"/pledge-payments/{payment.Id.Value}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PledgePaymentResponse>();
        Assert.NotNull(result);
        Assert.Equal(payment.Id.Value, result.Id);
        Assert.Equal(updatedPledgeId.Value, result.PledgeId);
        Assert.Equal(request.Amount, result.Amount);
        Assert.Equal(request.PaymentDate, result.PaymentDate);
        Assert.Equal(request.Comment, result.Comment);
        Assert.False(result.Active);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.True(result.UpdatedAt >= createdAt);

        var storedPayment = await repository.GetByIdAsync(payment.Id, CancellationToken.None);
        Assert.NotNull(storedPayment);
        Assert.Equal(updatedPledgeId, storedPayment.PledgeId);
        Assert.Equal(125.50m, storedPayment.Amount);
        Assert.False(storedPayment.Active);
        Assert.Equal(result.UpdatedAt, storedPayment.UpdatedAt);
    }

    [Fact]
    public async Task Put_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.PutAsJsonAsync(
            $"/pledge-payments/{Guid.NewGuid()}", CreateValidUpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_non_guid_id_returns_not_found()
    {
        using var client = CreateClient(_factory);

        using var response = await client.PutAsJsonAsync("/pledge-payments/not-a-guid", CreateValidUpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.PutAsJsonAsync(
            $"/pledge-payments/{Guid.Empty}", CreateValidUpdateRequest());

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge payment ID", problem.Title);
        Assert.Equal("Pledge payment ID cannot be empty.", problem.Detail);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"paymentDate":"2026-01-20T00:00:00","active":true}""")]
    [InlineData("""{"amount":100,"active":true}""")]
    [InlineData("""{"amount":100,"paymentDate":"2026-01-20T00:00:00"}""")]
    [InlineData("""{"pledgeId":"00000000-0000-0000-0000-000000000000","amount":100,"paymentDate":"2026-01-20T00:00:00","active":true}""")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","amount":0,"paymentDate":"2026-01-20T00:00:00","active":true}""")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","amount":1.001,"paymentDate":"2026-01-20T00:00:00","active":true}""")]
    [InlineData("""{"pledgeId":"6f1c2b9e-0d4a-4b8e-9a51-2f3c7d8e1a00","amount":100,"paymentDate":"0001-01-01T00:00:00","active":true}""")]
    public async Task Put_with_invalid_body_returns_bad_request_without_updating(string json)
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgePaymentRepository>();
        var payment = PledgePayment.Create(PledgeId.New(), 100m, new DateTime(2026, 1, 10), "Original");
        var originalUpdatedAt = payment.UpdatedAt;
        await repository.AddAsync(payment, CancellationToken.None);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"/pledge-payments/{payment.Id.Value}", content);

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge payment update", problem.Title);
        var storedPayment = await repository.GetByIdAsync(payment.Id, CancellationToken.None);
        Assert.NotNull(storedPayment);
        Assert.Equal(payment.PledgeId, storedPayment.PledgeId);
        Assert.Equal(100m, storedPayment.Amount);
        Assert.Equal(new DateTime(2026, 1, 10), storedPayment.PaymentDate);
        Assert.Equal("Original", storedPayment.Comment);
        Assert.True(storedPayment.Active);
        Assert.Equal(originalUpdatedAt, storedPayment.UpdatedAt);
    }

    [Fact]
    public async Task Put_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgePaymentRepository>(new FailingPledgePaymentRepository())));
        using var client = CreateClient(factory);

        using var response = await client.PutAsJsonAsync(
            $"/pledge-payments/{Guid.NewGuid()}", CreateValidUpdateRequest());

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge payment could not be updated.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Fact]
    public async Task Delete_with_existing_id_removes_pledge_payment_and_returns_no_content()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgePaymentRepository>();
        var payment = PledgePayment.Create(PledgeId.New(), 100m, new DateTime(2026, 1, 10), "Original");
        await repository.AddAsync(payment, CancellationToken.None);

        using var response = await client.DeleteAsync($"/pledge-payments/{payment.Id.Value}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Null(await repository.GetByIdAsync(payment.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_with_unknown_id_returns_not_found()
    {
        using var factory = CreateIsolatedFactory();
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledge-payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_with_non_guid_id_returns_not_found()
    {
        using var client = CreateClient(_factory);

        using var response = await client.DeleteAsync("/pledge-payments/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_with_empty_guid_returns_bad_request_problem_details()
    {
        using var client = CreateClient(_factory);

        using var response = await client.DeleteAsync($"/pledge-payments/{Guid.Empty}");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Invalid pledge payment ID", problem.Title);
        Assert.Equal("Pledge payment ID cannot be empty.", problem.Detail);
    }

    [Fact]
    public async Task Delete_when_repository_fails_returns_sanitized_internal_server_error()
    {
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgePaymentRepository>(new FailingPledgePaymentRepository())));
        using var client = CreateClient(factory);

        using var response = await client.DeleteAsync($"/pledge-payments/{Guid.NewGuid()}");

        var body = await response.Content.ReadAsStringAsync();
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        Assert.Equal("Pledge payment could not be removed.", problem.Detail);
        Assert.DoesNotContain("Internal repository failure", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    private static CreatePledgePaymentRequest CreateValidCreateRequest()
    {
        return new CreatePledgePaymentRequest
        {
            PledgeId = Guid.NewGuid(),
            Amount = 100m,
            PaymentDate = new DateTime(2026, 1, 20)
        };
    }

    private static UpdatePledgePaymentRequest CreateValidUpdateRequest()
    {
        return new UpdatePledgePaymentRequest
        {
            PledgeId = Guid.NewGuid(),
            Amount = 100m,
            PaymentDate = new DateTime(2026, 1, 20),
            Active = true
        };
    }

    private static async Task<PledgeId> SeedPaymentsAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPledgePaymentRepository>();
        var pledgeId = PledgeId.New();
        foreach (var (amount, paymentDate) in new[]
        {
            (100m, new DateTime(2026, 1, 3)),
            (150m, new DateTime(2026, 1, 1)),
            (50m, new DateTime(2026, 1, 2))
        })
        {
            await repository.AddAsync(
                PledgePayment.Create(pledgeId, amount, paymentDate), CancellationToken.None);
        }

        await repository.AddAsync(
            PledgePayment.Create(PledgeId.New(), 1m, new DateTime(2026, 1, 4)), CancellationToken.None);
        return pledgeId;
    }

    private WebApplicationFactory<Program> CreateIsolatedFactory()
    {
        return _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddSingleton<IPledgePaymentRepository, InMemoryPledgePaymentRepository>()));
    }

    private sealed class FailingPledgePaymentRepository : IPledgePaymentRepository
    {
        public Task AddAsync(PledgePayment pledgePayment, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<PledgePayment?> GetByIdAsync(PledgePaymentId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task<PagedPledgePaymentsResult> GetByPledgeAsync(
            PledgeId pledgeId, PledgePaymentQuery? query = null, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Internal repository failure");

        public Task UpdateAsync(PledgePayment pledgePayment, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");

        public Task RemoveAsync(PledgePaymentId id, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Internal repository failure");
    }
}
