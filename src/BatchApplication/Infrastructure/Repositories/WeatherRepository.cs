using System;
using System.Threading;
using System.Threading.Tasks;
using BatchApplication.Core.Common.Repository;
using BatchApplication.Core.Interfaces;
using BatchApplication.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BatchApplication.Infrastructure.Repositories;

public class WeatherRepository : BaseRepository<WeatherData, string>, IWeatherRepository
{
    private readonly DbContext _dbContext;
    private readonly ILogger<WeatherRepository> _logger;

    public WeatherRepository(
        DbContext context,
        ILogger<WeatherRepository> logger,
        RepositoryOptions options)
        : base(context, logger, options)
    {
        _dbContext = context;
        _logger = logger;
    }

    public async Task<WeatherData?> GetLatestByLocationAsync(string location, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                return await _dbContext.Set<WeatherData>()
                    .Where(w => w.Location == location)
                    .OrderByDescending(w => w.Timestamp)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest weather data for location: {Location}", location);
                throw;
            }
        });
    }

    public async Task<bool> UpdateWeatherDataAsync(WeatherData weatherData, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                var existing = await _dbContext.Set<WeatherData>()
                    .FirstOrDefaultAsync(w => w.Location == weatherData.Location, cancellationToken);

                if (existing != null)
                {
                    _dbContext.Entry(existing).CurrentValues.SetValues(weatherData);
                }
                else
                {
                    await _dbContext.Set<WeatherData>().AddAsync(weatherData, cancellationToken);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating weather data for location: {Location}", weatherData.Location);
                throw;
            }
        });
    }
}
