using ICFAccounting.Domain.Weather;

namespace ICFAccounting.Domain.Repositories;

public interface IWeatherForecastRepository
{
    Task<WeatherForecast[]> GetWeatherForecastAsync();
}