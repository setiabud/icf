using ICFAccounting.Domain.Repositories;
using ICFAccounting.Domain.Weather;

namespace ICFAccounting.Data.Repositories;

public sealed class InMemoryWeatherForecastRepository : IWeatherForecastRepository
{
    private readonly List<WeatherForecast> _forecasts;

    public InMemoryWeatherForecastRepository()
    {
        _forecasts =
        [
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now), 25, "Sunny"),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(1)), 22, "Partly Cloudy"),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(2)), 18, "Rainy"),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(3)), 30, "Hot"),
            new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(4)), 15, "Cool")
        ];
    }

    public Task<WeatherForecast[]> GetWeatherForecastAsync()
    {
        return Task.FromResult(_forecasts.ToArray());
    }
}