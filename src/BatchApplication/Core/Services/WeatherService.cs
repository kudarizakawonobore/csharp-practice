using System;
using System.Threading;
using System.Threading.Tasks;
using BatchApplication.Core.Interfaces;
using BatchApplication.Core.Models;
using Microsoft.Extensions.Logging;

namespace BatchApplication.Core.Services;

public class WeatherService
{
    private readonly IWeatherApiClient _weatherApiClient;
    private readonly IWeatherRepository _weatherRepository;
    private readonly ILogger<WeatherService> _logger;
    private readonly TimeSpan _dataStaleThreshold = TimeSpan.FromHours(1);

    public WeatherService(
        IWeatherApiClient weatherApiClient,
        IWeatherRepository weatherRepository,
        ILogger<WeatherService> logger)
    {
        _weatherApiClient = weatherApiClient;
        _weatherRepository = weatherRepository;
        _logger = logger;
    }

    public async Task<WeatherData> GetWeatherDataAsync(
        string location,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 最新のデータを取得
            var existingData = await _weatherRepository.GetLatestByLocationAsync(location, cancellationToken);
            
            // データが新しい場合はそのまま返す
            if (IsDataFresh(existingData))
            {
                _logger.LogInformation("Returning recent weather data for location: {Location}", location);
                return existingData!;
            }

            try
            {
                // 古いデータの場合はAPIから新しいデータを取得
                var newData = await _weatherApiClient.GetWeatherDataAsync(location, cancellationToken);
                await _weatherRepository.UpdateWeatherDataAsync(newData, cancellationToken);
                _logger.LogInformation("Updated weather data for location: {Location}", location);
                return newData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data from API for location: {Location}", location);
                
                // APIエラーの場合、古いデータがあればそれを返す
                if (existingData != null)
                {
                    _logger.LogWarning("Returning stale data for location: {Location}", location);
                    return existingData;
                }
                
                throw new Exception($"Unable to get weather data for location: {location}", ex);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetWeatherDataAsync for location: {Location}", location);
            throw;
        }
    }

    private bool IsDataFresh(WeatherData? data)
    {
        if (data == null) return false;
        
        var age = DateTime.UtcNow - data.Timestamp;
        return age <= _dataStaleThreshold;
    }
}
