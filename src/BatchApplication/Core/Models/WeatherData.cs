using System;

namespace BatchApplication.Core.Models;

public class WeatherData
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
