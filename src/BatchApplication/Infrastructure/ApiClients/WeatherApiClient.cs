using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BatchApplication.Core.Common.ApiClient;
using BatchApplication.Core.Interfaces;
using BatchApplication.Core.Models;
using Microsoft.Extensions.Logging;

namespace BatchApplication.Infrastructure.ApiClients;

public class WeatherApiClient : BaseApiClient, IWeatherApiClient
{
    public WeatherApiClient(
        HttpClient httpClient,
        ILogger<WeatherApiClient> logger,
        ApiClientOptions options)
        : base(httpClient, logger, options)
    {
    }

    public async Task<WeatherData> GetWeatherDataAsync(string location, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching weather data for location: {Location}", location);
            var result = await GetAsync<WeatherData>($"weather/{location}", cancellationToken);
            result.LastUpdated = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weather data for location: {Location}", location);
            throw;
        }
    }
}
