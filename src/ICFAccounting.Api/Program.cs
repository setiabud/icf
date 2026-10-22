using ICFAccounting.Api.Extensions;

namespace ICFAccounting.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddApplicationServices() 
            .AddApplicationLogging()
            .AddOpenApiDocumentation();

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseApplicationLogging();
        app.UseOpenApiDocumentation();
        app.MapEndpoints();

        app.Run();
    }
}