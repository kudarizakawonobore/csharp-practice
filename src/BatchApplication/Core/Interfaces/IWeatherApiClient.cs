using System.Threading;
using System.Threading.Tasks;
using BatchApplication.Core.Common.ApiClient;
using BatchApplication.Core.Models;

namespace BatchApplication.Core.Interfaces;

public interface IWeatherApiClient : IApiClient
{
    Task<WeatherData> GetWeatherDataAsync(string location, CancellationToken cancellationToken = default);
}
