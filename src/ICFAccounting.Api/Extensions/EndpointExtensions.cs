using ICFAccounting.Application.Services;

namespace ICFAccounting.Api.Extensions;

public static class EndpointExtensions
{
    public static void MapEndpoints(this WebApplication app)
    {
        app.MapGet("/weatherforecast", GetWeatherForecastAsync)
            .WithName("GetWeatherForecast")
            .WithSummary("Get Weather Forecast");
    }
    private static async Task<IResult> GetWeatherForecastAsync(
        IWeatherService weatherService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var weatherForecast = await weatherService.GetWeatherForecastAsync();
            return Results.Ok(weatherForecast);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An unexpected error occurred while retrieving tasks.");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Weather forecasts could not be retrieved.");
        }
    }
}