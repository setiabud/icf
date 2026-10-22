using ICFAccounting.Domain.Weather;

namespace ICFAccounting.Application.Services;

public interface IWeatherService
{
    Task<WeatherForecast[]> GetWeatherForecastAsync();
}