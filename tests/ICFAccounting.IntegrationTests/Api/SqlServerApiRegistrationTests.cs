using System.Net;
using System.Net.Http.Json;
using ICFAccounting.Api.Models.PledgeCategoryModels;
using ICFAccounting.Api.Models.PledgeModels;
using ICFAccounting.Api.Models.PledgePaymentModels;
using ICFAccounting.Api.Models.PledgerModels;
using ICFAccounting.Data;
using ICFAccounting.Data.Repositories;
using ICFAccounting.Domain.Repositories;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using static ICFAccounting.IntegrationTests.Api.ApiTestHelpers;

namespace ICFAccounting.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class SqlServerApiRegistrationTests(SqlServerModelFixture fixture) : IAsyncLifetime
{
    private readonly string _databaseName = $"ICFAccountingApiTests_{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        await using var context = fixture.CreateContext(_databaseName);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = fixture.CreateContext(_databaseName);
        await context.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Default_startup_uses_sql_and_persists_all_aggregates_across_host_restarts()
    {
        PledgerResponse pledger;
        PledgeCategoryResponse category;
        PledgeResponse pledge;
        PledgePaymentResponse payment;
        using (var factory = CreateFactory(_databaseName))
        {
            using var client = CreateClient(factory, allowAutoRedirect: false);
            using var scope = factory.Services.CreateScope();
            Assert.IsType<SqlServerPledgerRepository>(scope.ServiceProvider.GetRequiredService<IPledgerRepository>());
            Assert.IsType<SqlServerPledgeCategoryRepository>(scope.ServiceProvider.GetRequiredService<IPledgeCategoryRepository>());
            Assert.IsType<SqlServerPledgeRepository>(scope.ServiceProvider.GetRequiredService<IPledgeRepository>());
            Assert.IsType<SqlServerPledgePaymentRepository>(scope.ServiceProvider.GetRequiredService<IPledgePaymentRepository>());
            Assert.Equal(_databaseName,
                scope.ServiceProvider.GetRequiredService<ICFAccountingDbContext>().Database.GetDbConnection().Database);

            pledger = await PostCreatedAsync<PledgerResponse>(client, "/pledgers",
                new CreatePledgerRequest { PledgerName = "SQL API pledger" });
            category = await PostCreatedAsync<PledgeCategoryResponse>(client, "/pledge-categories",
                new CreatePledgeCategoryRequest { CategoryName = "SQL API category" });
            pledge = await PostCreatedAsync<PledgeResponse>(client, "/pledges", new CreatePledgeRequest
            {
                Year = 2026,
                Amount = 125.50m,
                PledgerId = pledger.Id,
                PledgeCategoryId = category.Id,
                Comment = "API SQL persistence"
            });
            payment = await PostCreatedAsync<PledgePaymentResponse>(client, "/pledge-payments", new CreatePledgePaymentRequest
            {
                PledgeId = pledge.Id,
                Amount = 25.50m,
                PaymentDate = new DateTime(2026, 10, 27),
                Comment = "API SQL payment"
            });
        }

        using var restartedFactory = CreateFactory(_databaseName);
        using var restartedClient = CreateClient(restartedFactory);
        Assert.Equal(pledger, await restartedClient.GetFromJsonAsync<PledgerResponse>($"/pledgers/{pledger.Id}"));
        Assert.Equal(category, await restartedClient.GetFromJsonAsync<PledgeCategoryResponse>($"/pledge-categories/{category.Id}"));
        Assert.Equal(pledge, await restartedClient.GetFromJsonAsync<PledgeResponse>($"/pledges/{pledge.Id}"));
        Assert.Equal(payment, await restartedClient.GetFromJsonAsync<PledgePaymentResponse>($"/pledge-payments/{payment.Id}"));

        await using var readContext = fixture.CreateContext(_databaseName);
        Assert.Equal(1, await readContext.Pledgers.CountAsync());
        Assert.Equal(1, await readContext.PledgeCategories.CountAsync());
        Assert.Equal(1, await readContext.Pledges.CountAsync());
        Assert.Equal(1, await readContext.PledgePayments.CountAsync());
    }

    [Fact]
    public async Task Startup_does_not_create_or_migrate_a_database()
    {
        var uninitializedDatabase = $"ICFAccountingUninitialized_{Guid.NewGuid():N}";
        using var factory = CreateFactory(uninitializedDatabase);
        using var client = CreateClient(factory);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ICFAccountingDbContext>();
        Assert.Equal(uninitializedDatabase, context.Database.GetDbConnection().Database);
        Assert.False(await context.Database.CanConnectAsync());
    }

    [Theory]
    [InlineData("Pledger", false)]
    [InlineData("Pledger", true)]
    [InlineData("Category", false)]
    [InlineData("Category", true)]
    [InlineData("Pledge", false)]
    [InlineData("Pledge", true)]
    public async Task Missing_reference_writes_return_sanitized_conflict_without_partial_changes(
        string missingReference, bool update)
    {
        using var factory = CreateFactory(_databaseName);
        using var client = CreateClient(factory);
        var (pledger, category, pledge, payment) = await CreateRelatedRecordsAsync(client);
        var path = missingReference == "Pledge" ? "/pledge-payments" : "/pledges";
        var identity = missingReference == "Pledge" ? payment.Id : pledge.Id;
        object request = missingReference == "Pledge" ? new
        {
            PledgeId = Guid.NewGuid(), Amount = 20m, PaymentDate = new DateTime(2026, 10, 27),
            Active = false, Comment = "Rejected payment"
        } : new
        {
            Year = 2027, Amount = 50m, Comment = "Rejected pledge",
            PledgerId = missingReference == "Pledger" ? Guid.NewGuid() : pledger.Id,
            PledgeCategoryId = missingReference == "Category" ? Guid.NewGuid() : category.Id
        };

        using var response = update
            ? await client.PutAsJsonAsync($"{path}/{identity}", request)
            : await client.PostAsJsonAsync(path, request);

        await AssertSanitizedConflictAsync(response);
        Assert.Equal(pledge, await client.GetFromJsonAsync<PledgeResponse>($"/pledges/{pledge.Id}"));
        Assert.Equal(payment, await client.GetFromJsonAsync<PledgePaymentResponse>($"/pledge-payments/{payment.Id}"));
        await using var context = fixture.CreateContext(_databaseName);
        Assert.Equal(1, await context.Pledges.CountAsync());
        Assert.Equal(1, await context.PledgePayments.CountAsync());
    }

    [Theory]
    [InlineData("Pledger")]
    [InlineData("Category")]
    [InlineData("Pledge")]
    public async Task Referenced_deletes_return_conflict_and_succeed_after_dependencies_are_removed(string aggregate)
    {
        using var factory = CreateFactory(_databaseName);
        using var client = CreateClient(factory);
        var (pledger, category, pledge, payment) = await CreateRelatedRecordsAsync(client);
        var path = aggregate switch
        {
            "Pledger" => $"/pledgers/{pledger.Id}",
            "Category" => $"/pledge-categories/{category.Id}",
            _ => $"/pledges/{pledge.Id}"
        };

        using var response = await client.DeleteAsync(path);
        await AssertSanitizedConflictAsync(response);
        Assert.Equal(pledger, await client.GetFromJsonAsync<PledgerResponse>($"/pledgers/{pledger.Id}"));
        Assert.Equal(category, await client.GetFromJsonAsync<PledgeCategoryResponse>($"/pledge-categories/{category.Id}"));
        Assert.Equal(pledge, await client.GetFromJsonAsync<PledgeResponse>($"/pledges/{pledge.Id}"));
        Assert.Equal(payment, await client.GetFromJsonAsync<PledgePaymentResponse>($"/pledge-payments/{payment.Id}"));

        foreach (var allowedPath in new[]
        {
            $"/pledge-payments/{payment.Id}", $"/pledges/{pledge.Id}",
            $"/pledgers/{pledger.Id}", $"/pledge-categories/{category.Id}"
        })
        {
            using var deleted = await client.DeleteAsync(allowedPath);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        }

        using var missing = await client.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("/pledgers", false)]
    [InlineData("/pledgers", true)]
    [InlineData("/pledge-categories", false)]
    [InlineData("/pledge-categories", true)]
    [InlineData("/pledges", false)]
    [InlineData("/pledges", true)]
    [InlineData("/pledge-payments", false)]
    [InlineData("/pledge-payments", true)]
    public async Task Missing_write_targets_still_return_not_found(string path, bool update)
    {
        using var factory = CreateFactory(_databaseName);
        using var client = CreateClient(factory);
        var target = $"{path}/{Guid.NewGuid()}";
        var request = new
        {
            PledgerName = "Missing", CategoryName = "Missing", Active = true,
            Year = 2026, Amount = 100m, PledgerId = Guid.NewGuid(),
            PledgeCategoryId = Guid.NewGuid(), PledgeId = Guid.NewGuid(),
            PaymentDate = new DateTime(2026, 10, 27)
        };

        using var response = update
            ? await client.PutAsJsonAsync(target, request) : await client.DeleteAsync(target);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Check_violations_are_not_misclassified_as_relationship_conflicts(bool synchronous)
    {
        await using (var context = fixture.CreateContext(_databaseName))
        {
            await context.Database.ExecuteSqlRawAsync(
                "ALTER TABLE [dbo].[Pledgers] ADD CONSTRAINT [CK_Pledgers_RejectTestName] CHECK ([PledgerName] <> N'RejectedByCheck')");
            context.Pledgers.Add(ICFAccounting.Domain.Pledgers.Pledger.Create("RejectedByCheck"));
            var exception = synchronous
                ? Assert.Throws<DbUpdateException>(() => context.SaveChanges())
                : await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            Assert.Equal(547, Assert.IsType<SqlException>(exception.InnerException).Number);
        }

        using var factory = CreateFactory(_databaseName);
        using var client = CreateClient(factory);
        using var response = await client.PostAsJsonAsync("/pledgers",
            new CreatePledgerRequest { PledgerName = "RejectedByCheck" });
        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred", problem.Title);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("CK_Pledgers_RejectTestName", body);
        Assert.DoesNotContain("SqlException", body);
        Assert.DoesNotContain(_databaseName, body);
    }

    [Theory]
    [InlineData("/pledges", "POST")]
    [InlineData("/pledges/{id:guid}", "PUT")]
    [InlineData("/pledges/{id:guid}", "DELETE")]
    [InlineData("/pledge-payments", "POST")]
    [InlineData("/pledge-payments/{id:guid}", "PUT")]
    [InlineData("/pledgers/{id:guid}", "DELETE")]
    [InlineData("/pledge-categories/{id:guid}", "DELETE")]
    public void Conflicting_write_routes_advertise_problem_details(string path, string method)
    {
        using var factory = CreateFactory(_databaseName);
        using var client = CreateClient(factory);
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        var endpoint = Assert.Single(source.Endpoints.OfType<RouteEndpoint>(), candidate =>
            candidate.RoutePattern.RawText == path
            && candidate.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains(method));
        var response = Assert.Single(endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            metadata => metadata.StatusCode == 409);
        Assert.Contains("application/problem+json", response.ContentTypes);
    }

    private static async Task<(PledgerResponse Pledger, PledgeCategoryResponse Category,
        PledgeResponse Pledge, PledgePaymentResponse Payment)> CreateRelatedRecordsAsync(HttpClient client)
    {
        var pledger = await PostCreatedAsync<PledgerResponse>(client, "/pledgers",
            new CreatePledgerRequest { PledgerName = "Conflict test pledger" });
        var category = await PostCreatedAsync<PledgeCategoryResponse>(client, "/pledge-categories",
            new CreatePledgeCategoryRequest { CategoryName = "Conflict test category" });
        var pledge = await PostCreatedAsync<PledgeResponse>(client, "/pledges", new CreatePledgeRequest
        {
            Year = 2026, Amount = 100m, PledgerId = pledger.Id, PledgeCategoryId = category.Id
        });
        var payment = await PostCreatedAsync<PledgePaymentResponse>(client, "/pledge-payments", new CreatePledgePaymentRequest
        {
            PledgeId = pledge.Id, Amount = 10m, PaymentDate = new DateTime(2026, 10, 27)
        });
        return (pledger, category, pledge, payment);
    }

    private async Task AssertSanitizedConflictAsync(HttpResponseMessage response)
    {
        var problem = await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Equal("Relationship conflict", problem.Title);
        Assert.Equal("The operation conflicts with related accounting records.", problem.Detail);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("FK_", body);
        Assert.DoesNotContain("SqlException", body);
        Assert.DoesNotContain(_databaseName, body);
    }

    private SqlServerApiFactory CreateFactory(string databaseName)
    {
        using var context = fixture.CreateContext(databaseName);
        return new SqlServerApiFactory(context.Database.GetDbConnection().ConnectionString);
    }

    private static async Task<TResponse> PostCreatedAsync<TResponse>(HttpClient client, string path, object request)
    {
        using var response = await client.PostAsJsonAsync(path, request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TResponse>();
        Assert.NotNull(result);
        return result;
    }
}