using System.Threading;
using System.Threading.Tasks;

namespace BatchApplication.Core.Common.ApiClient;

public interface IApiClient
{
    Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
    Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
}
