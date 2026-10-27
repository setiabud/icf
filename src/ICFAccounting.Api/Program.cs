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
            .AddOpenApiDocumentation();

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseStatusCodePages();
        app.UseApplicationLogging();
        app.UseOpenApiDocumentation();
        app.MapEndpoints();

        app.Run();
    }
}