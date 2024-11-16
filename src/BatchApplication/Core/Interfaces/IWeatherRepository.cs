using System.Threading;
using System.Threading.Tasks;
using BatchApplication.Core.Common.Repository;
using BatchApplication.Core.Models;

namespace BatchApplication.Core.Interfaces;

public interface IWeatherRepository : IRepository<WeatherData, string>
{
    Task<WeatherData?> GetLatestByLocationAsync(string location, CancellationToken cancellationToken = default);
    Task<bool> UpdateWeatherDataAsync(WeatherData weatherData, CancellationToken cancellationToken = default);
}
