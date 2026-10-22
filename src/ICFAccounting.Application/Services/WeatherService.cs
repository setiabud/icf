using ICFAccounting.Domain.Repositories;
using ICFAccounting.Domain.Weather;

namespace ICFAccounting.Application.Services;

public sealed class WeatherService : IWeatherService
{
    private readonly IWeatherForecastRepository _weatherForecastRepository;

    public WeatherService(IWeatherForecastRepository weatherForecastRepository)
    {
        _weatherForecastRepository = weatherForecastRepository;
    }

    public async Task<WeatherForecast[]> GetWeatherForecastAsync()
    {
        return await _weatherForecastRepository.GetWeatherForecastAsync();
    }
}