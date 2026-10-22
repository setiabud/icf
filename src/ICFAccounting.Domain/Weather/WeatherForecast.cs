namespace ICFAccounting.Domain.Weather;

public sealed class WeatherForecast(DateOnly date, int temperatureC, string? summary)
{
    public DateOnly Date { get; } = date;
    public int TemperatureC { get; } = temperatureC;
    public string? Summary { get; } = summary;

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}