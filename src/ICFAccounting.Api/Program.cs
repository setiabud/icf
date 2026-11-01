using ICFAccounting.Api.Extensions;
using ICFAccounting.Data.Extensions;

namespace ICFAccounting.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddDataServices(builder.Configuration)
            .AddApplicationServices()
            .AddApplicationLogging()
            .AddOpenApiDocumentation()
            .AddDevelopmentCors(builder.Configuration, builder.Environment);

        var app = builder.Build();

        // CORS runs before HTTPS redirection so preflight requests are not redirected.
        app.UseDevelopmentCors();
        app.UseHttpsRedirection();
        app.UseStatusCodePages();
        app.UseApplicationLogging();
        app.UseOpenApiDocumentation();
        app.MapEndpoints();

        app.Run();
    }
}