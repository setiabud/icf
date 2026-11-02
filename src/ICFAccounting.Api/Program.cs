using ICFAccounting.Api.Extensions;
using ICFAccounting.Api.Authentication;
using ICFAccounting.Data.Extensions;

namespace ICFAccounting.Api;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var isUserCommand = args.FirstOrDefault() == "users";
        if (isUserCommand && args.Length != 2)
        {
            Console.Error.WriteLine("Use users create or users set-password without additional arguments.");
            return 1;
        }
        var builder = WebApplication.CreateBuilder(isUserCommand ? [] : args);

        builder.Services
            .AddDataServices(builder.Configuration)
            .AddAccountAuthentication(builder.Configuration)
            .AddApplicationServices()
            .AddApplicationLogging()
            .AddOpenApiDocumentation()
            .AddDevelopmentCors(builder.Configuration, builder.Environment);

        await using var app = builder.Build();

        if (isUserCommand)
        {
            return await UserAccountCommand.RunAsync(args.ElementAtOrDefault(1) ?? "", app.Services);
        }

        // CORS runs before HTTPS redirection so preflight requests are not redirected.
        app.UseDevelopmentCors();
        app.UseHttpsRedirection();
        app.UseStatusCodePages();
        app.UseApplicationLogging();
        app.UseAccountAuthentication();
        app.UseOpenApiDocumentation();
        app.MapAuthenticationEndpoints();
        app.MapEndpoints();

        await app.RunAsync();
        return 0;
    }
}