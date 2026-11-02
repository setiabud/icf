using System.Net.Mail;
using ICFAccounting.Data.Authentication;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace ICFAccounting.Api.Authentication;

public static class AuthenticationEndpoints
{
    public static void MapAuthenticationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/auth").WithTags("Authentication");
        group.AddEndpointFilter(async (context, next) =>
        {
            try
            {
                return await next(context);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>()
                    .LogError(exception, "An authentication operation failed.");
                return Results.Problem(statusCode: 503, title: "Authentication unavailable",
                    detail: "Sign-in services are temporarily unavailable. Try again later.");
            }
        });
        group.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Ok(new { requestToken = antiforgery.GetAndStoreTokens(context).RequestToken }))
            .AllowAnonymous()
            .WithSummary("Issue a request verification token");
        group.MapPost("/login", LoginAsync).AllowAnonymous().RequireRateLimiting("login")
            .WithSummary("Sign in with administrator-provisioned credentials")
            .Produces<SessionResponse>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(429);
        group.MapGet("/session", SessionAsync).RequireAuthorization()
            .WithSummary("Restore the current session").Produces<SessionResponse>().ProducesProblem(401);
        group.MapPost("/logout", async (SignInManager<UserAccount> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization().WithSummary("End the current browser session").Produces(204).ProducesProblem(401);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, UserManager<UserAccount> users, SignInManager<UserAccount> signInManager,
        UserProfileStore profiles, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > 256 ||
            !MailAddress.TryCreate(email, out var address) || address.Address != email ||
            string.IsNullOrEmpty(request.Password) || request.Password.Length > 1024)
        {
            return Results.Problem(statusCode: 400, title: "Email and password are required",
                detail: "Enter a valid email address and a password of no more than 1024 characters.");
        }

        var account = await users.FindByEmailAsync(email);
        if (account is null)
        {
            // Perform the same expensive hash operation without revealing whether an email is registered.
            _ = users.PasswordHasher.HashPassword(new UserAccount(), request.Password);
            return InvalidCredentials();
        }

        var result = await signInManager.CheckPasswordSignInAsync(account, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        var profile = await profiles.GetOrCreateAsync(account, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        await signInManager.SignInAsync(account, new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = false,
            ExpiresUtc = expiresAt
        });
        return Results.Ok(ToResponse(account, profile, expiresAt));
    }

    private static async Task<IResult> SessionAsync(
        HttpContext context, UserManager<UserAccount> users, UserProfileStore profiles, CancellationToken cancellationToken)
    {
        var account = await users.GetUserAsync(context.User);
        var ticket = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (account is null || ticket.Properties?.ExpiresUtc is not { } expiresAt)
        {
            return Results.Problem(statusCode: 401, title: "Sign-in required",
                detail: "Your session is missing or expired. Sign in to continue.");
        }
        var profile = await profiles.FindAsync(account.Id, cancellationToken);
        return profile is null
            ? Results.Problem(statusCode: 401, title: "Sign-in required", detail: "Sign in to initialize your application profile.")
            : Results.Ok(ToResponse(account, profile, expiresAt));
    }

    private static SessionResponse ToResponse(UserAccount account, UserProfile profile, DateTimeOffset expiresAt) => new(
        profile.Id, profile.Name, account.Email!, profile.ProfileImageUrl, expiresAt);

    private static IResult InvalidCredentials() => Results.Problem(statusCode: 401, title: "Sign-in failed",
        detail: "Email or password is incorrect, or the account is temporarily locked. Try again later or contact your administrator.");
}
