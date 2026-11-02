using System.Net;
using System.Net.Http.Json;
using System.Buffers.Binary;
using ICFAccounting.Data.Authentication;
using ICFAccounting.IntegrationTests.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ICFAccounting.IntegrationTests.Api;

[Collection(nameof(SqlServerTestCollection))]
public sealed class AuthenticationEndpointsTests(SqlServerModelFixture fixture) : SqlServerApiTestBase(fixture)
{
    private const string PASSWORD = "Test-only-Password-83!";

    [Theory]
    [InlineData("/pledgers")]
    [InlineData("/pledge-categories")]
    [InlineData("/pledges/00000000-0000-0000-0000-000000000001")]
    [InlineData("/pledge-payments/00000000-0000-0000-0000-000000000001")]
    [InlineData("/auth/session")]
    public async Task Anonymous_requests_are_unauthorized_without_redirects(string path)
    {
        using var client = CreateAnonymousClient();
        using var response = await client.GetAsync(path);
        await ApiTestHelpers.AssertProblemAsync(response, HttpStatusCode.Unauthorized);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Every_accounting_write_requires_backend_authentication()
    {
        using var client = CreateAnonymousClient();
        foreach (var resource in new[] { "pledgers", "pledge-categories", "pledges", "pledge-payments" })
        {
            foreach (var method in new[] { HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete })
            {
                var path = method == HttpMethod.Post ? $"/{resource}" : $"/{resource}/{Guid.NewGuid()}";
                using var request = new HttpRequestMessage(method, path);
                using var response = await client.SendAsync(request);
                await ApiTestHelpers.AssertProblemAsync(response, HttpStatusCode.Unauthorized);
            }
        }
    }

    [Fact]
    public async Task First_login_activates_profile_and_returning_login_reuses_user_with_hashed_password()
    {
        var id = await ProvisionAsync();
        using var client = CreateAnonymousClient();
        using var login = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("__Host-ICFAccounting.Session="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
        var firstProfile = await login.Content.ReadFromJsonAsync<Profile>();
        Assert.NotNull(firstProfile);
        Assert.Equal(id, firstProfile.Id);
        Assert.Equal("Test Member", firstProfile.Name);
        Assert.Equal("https://images.example.test/member.png", firstProfile.ProfileImageUrl);
        DateTimeOffset profileCreatedAt;
        using (var firstScope = _factory.Services.CreateScope())
        {
            profileCreatedAt = (await firstScope.ServiceProvider.GetRequiredService<AuthenticationDbContext>()
                .UserProfiles.SingleAsync()).CreatedAt;
        }

        using var restored = await client.GetAsync("/auth/session");
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        Assert.Equal(firstProfile.Id, (await restored.Content.ReadFromJsonAsync<Profile>())!.Id);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/pledgers")).StatusCode);
        using var created = await SendWithTokenAsync(client, "/pledgers", new { pledgerName = "Authenticated member" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        using var secondClient = CreateAnonymousClient();
        using var returning = await LoginAsync(secondClient);
        Assert.Equal(id, (await returning.Content.ReadFromJsonAsync<Profile>())!.Id);
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        var user = await database.Users.SingleAsync();
        var storedProfile = await database.UserProfiles.SingleAsync();
        Assert.Equal(id, storedProfile.Id);
        Assert.Equal(profileCreatedAt, storedProfile.CreatedAt);
        Assert.NotEqual(PASSWORD, user.PasswordHash);
        var hash = Convert.FromBase64String(user.PasswordHash!);
        Assert.Equal(1, hash[0]);
        Assert.True(BinaryPrimitives.ReadUInt32BigEndian(hash.AsSpan(5, 4)) >= 210_000);
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
        Assert.True(await manager.CheckPasswordAsync(user, PASSWORD));
    }

    [Fact]
    public async Task Logout_removes_session_and_requires_authentication_again()
    {
        await ProvisionAsync();
        using var client = CreateAnonymousClient();
        (await LoginAsync(client)).EnsureSuccessStatusCode();
        using var response = await SendWithTokenAsync(client, "/auth/logout");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/pledgers")).StatusCode);
    }

    [Fact]
    public async Task Expired_cookie_cannot_restore_session_or_access_accounting_data()
    {
        await ProvisionAsync();
        using var scope = _factory.Services.CreateScope();
        var clock = new SessionTestClock();
        var options = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        options.TimeProvider = clock;
        using var client = CreateAnonymousClient();
        (await LoginAsync(client)).EnsureSuccessStatusCode();
        clock.Advance(TimeSpan.FromHours(7));
        using var active = await client.GetAsync("/auth/session");
        Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        Assert.False(active.Headers.TryGetValues("Set-Cookie", out var cookies) &&
            cookies.Any(cookie => cookie.StartsWith("__Host-ICFAccounting.Session=")));
        clock.Advance(TimeSpan.FromHours(1).Add(TimeSpan.FromSeconds(1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/pledgers")).StatusCode);
    }

    [Fact]
    public async Task Failed_password_and_unknown_account_have_generic_errors_and_do_not_activate_profiles()
    {
        await ProvisionAsync();
        using var client = CreateAnonymousClient();
        using var wrong = await LoginAsync(client, "wrong-password");
        using var unknown = await SendWithTokenAsync(client, "/auth/login",
            new { email = "unknown@example.test", password = PASSWORD });
        var first = await ApiTestHelpers.AssertProblemAsync(wrong, HttpStatusCode.Unauthorized);
        var second = await ApiTestHelpers.AssertProblemAsync(unknown, HttpStatusCode.Unauthorized);
        Assert.Equal(first.Title, second.Title);
        Assert.Equal(first.Detail, second.Detail);
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        Assert.Single(await database.Users.ToListAsync());
        Assert.Empty(await database.UserProfiles.ToListAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Repeated_failed_passwords_lock_account_even_when_password_is_correct()
    {
        await ProvisionAsync();
        using var client = CreateAnonymousClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await LoginAsync(client, "wrong-password");
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }
        using var locked = await LoginAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [Fact]
    public async Task Unsafe_requests_require_valid_antiforgery_tokens_including_login_and_logout()
    {
        await ProvisionAsync();
        using var client = CreateAnonymousClient();
        using var login = await client.PostAsJsonAsync("/auth/login",
            new { email = "member@example.test", password = PASSWORD });
        await ApiTestHelpers.AssertProblemAsync(login, HttpStatusCode.BadRequest);
        var anonymousToken = await client.GetFromJsonAsync<Token>("/auth/csrf");
        (await LoginAsync(client)).EnsureSuccessStatusCode();
        using var write = await client.PostAsJsonAsync("/pledgers", new { pledgerName = "Rejected" });
        await ApiTestHelpers.AssertProblemAsync(write, HttpStatusCode.BadRequest);
        using var logout = await client.PostAsync("/auth/logout", null);
        await ApiTestHelpers.AssertProblemAsync(logout, HttpStatusCode.BadRequest);
        using var staleToken = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
        staleToken.Headers.Add("X-CSRF-TOKEN", anonymousToken!.RequestToken);
        using var staleResponse = await client.SendAsync(staleToken);
        await ApiTestHelpers.AssertProblemAsync(staleResponse, HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/auth/session")).StatusCode);
    }

    [Fact]
    public async Task Login_rate_limit_returns_problem_details_and_retry_guidance()
    {
        using var client = CreateAnonymousClient();
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var invalid = await SendWithTokenAsync(client, "/auth/login", new { email = "", password = "" });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using var limited = await SendWithTokenAsync(client, "/auth/login", new { email = "", password = "" });
        await ApiTestHelpers.AssertProblemAsync(limited, HttpStatusCode.TooManyRequests);
        Assert.Equal(TimeSpan.FromSeconds(60), limited.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task Concurrent_first_signins_create_exactly_one_application_user()
    {
        var id = await ProvisionAsync();
        using var first = _factory.Services.CreateScope();
        using var second = _factory.Services.CreateScope();
        var firstAccount = (await first.ServiceProvider.GetRequiredService<UserManager<UserAccount>>().FindByIdAsync(id))!;
        var secondAccount = (await second.ServiceProvider.GetRequiredService<UserManager<UserAccount>>().FindByIdAsync(id))!;
        var profiles = await Task.WhenAll(
            first.ServiceProvider.GetRequiredService<UserProfileStore>().GetOrCreateAsync(firstAccount, CancellationToken.None),
            second.ServiceProvider.GetRequiredService<UserProfileStore>().GetOrCreateAsync(secondAccount, CancellationToken.None));
        Assert.Equal(profiles[0].Id, profiles[1].Id);
        Assert.Equal(profiles[0].CreatedAt, profiles[1].CreatedAt);
        using var verification = _factory.Services.CreateScope();
        Assert.Equal(1, await verification.ServiceProvider.GetRequiredService<AuthenticationDbContext>().UserProfiles.CountAsync());
    }

    [Fact]
    public async Task Provisioning_rejects_weak_passwords_and_duplicate_email()
    {
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>().Database.MigrateAsync();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
        var weak = await manager.CreateAsync(
            new UserAccount { UserName = "weak@example.test", Email = "weak@example.test" }, "short");
        Assert.False(weak.Succeeded);
        Assert.Contains(weak.Errors, error => error.Code == "PasswordTooShort");
        var existing = new UserAccount { UserName = "first@example.test", Email = "first@example.test" };
        Assert.True((await manager.CreateAsync(existing, PASSWORD)).Succeeded);
        var duplicate = await manager.CreateAsync(
            new UserAccount { UserName = "second@example.test", Email = "FIRST@example.test" }, PASSWORD);
        Assert.False(duplicate.Succeeded);
        Assert.Contains(duplicate.Errors, error => error.Code == "DuplicateEmail");
    }

    [Fact]
    public async Task Identity_migrations_are_isolated_repeatable_and_can_roll_back_without_dropping_accounting()
    {
        await ProvisionAsync();
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        await database.Database.MigrateAsync();
        Assert.False(database.Database.HasPendingModelChanges());
        Assert.Equal(database.Database.GetMigrations(), await database.Database.GetAppliedMigrationsAsync());
        Assert.Single(await database.Users.ToListAsync());
        await database.GetService<IMigrator>().MigrateAsync(Migration.InitialDatabase);
        Assert.Empty(await database.Database.GetAppliedMigrationsAsync());
        await using var accounting = CreateContext();
        Assert.NotEmpty(await accounting.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await accounting.Pledgers.ToListAsync());
        await database.Database.MigrateAsync();
        database.ChangeTracker.Clear();
        Assert.Empty(await database.Users.ToListAsync());
        Assert.Empty(await database.UserProfiles.ToListAsync());
    }

    [Fact]
    public async Task Password_reset_revokes_existing_sessions_and_registration_is_not_exposed()
    {
        await ProvisionAsync();
        using var client = CreateAnonymousClient();
        (await LoginAsync(client)).EnsureSuccessStatusCode();
        using (var scope = _factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
            var user = (await manager.FindByEmailAsync("member@example.test"))!;
            var token = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.True((await manager.ResetPasswordAsync(user, token, "Another-Test-Password-83!")).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/auth/session")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(client)).StatusCode);
        using var registration = await SendWithTokenAsync(client, "/auth/register",
            new { email = "new@example.test", password = PASSWORD });
        Assert.NotEqual(HttpStatusCode.OK, registration.StatusCode);
        Assert.NotEqual(HttpStatusCode.Created, registration.StatusCode);
    }

    private HttpClient CreateAnonymousClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });

    private async Task<string> ProvisionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
        await database.Database.MigrateAsync();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
        var user = new UserAccount
        {
            UserName = "member@example.test", Email = "member@example.test", DisplayName = "Test Member",
            ProfileImageUrl = "https://images.example.test/member.png"
        };
        var result = await manager.CreateAsync(user, PASSWORD);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        Assert.Empty(await database.UserProfiles.ToListAsync());
        return user.Id;
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string password = PASSWORD) =>
        SendWithTokenAsync(client, "/auth/login", new { email = "member@example.test", password });

    private static async Task<HttpResponseMessage> SendWithTokenAsync(HttpClient client, string path, object? body = null)
    {
        var token = await client.GetFromJsonAsync<Token>("/auth/csrf");
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Add("X-CSRF-TOKEN", token!.RequestToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return await client.SendAsync(request);
    }

    private sealed record Token(string RequestToken);
    private sealed record Profile(string Id, string Name, string? ProfileImageUrl);

    private sealed class SessionTestClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
