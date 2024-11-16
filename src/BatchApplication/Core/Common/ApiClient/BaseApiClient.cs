using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BatchApplication.Core.Common.ApiClient;

public abstract class BaseApiClient : IApiClient
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected readonly ApiClientOptions _options;

    protected BaseApiClient(
        HttpClient httpClient,
        ILogger logger,
        ApiClientOptions options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = _options.Timeout;

        foreach (var header in _options.DefaultHeaders)
        {
            _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    public async Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            return await HandleResponseAsync<TResponse>(response);
        });
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
            return await HandleResponseAsync<TResponse>(response);
        });
    }

    public async Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, request, cancellationToken);
            return await HandleResponseAsync<TResponse>(response);
        });
    }

    public async Task<TResponse> DeleteAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.DeleteAsync(endpoint, cancellationToken);
            return await HandleResponseAsync<TResponse>(response);
        });
    }

    protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetry(ex, retryCount))
            {
                retryCount++;
                _logger.LogWarning(ex, "Retry attempt {RetryCount} of {MaxRetries}", 
                    retryCount, _options.MaxRetryAttempts);
                
                await Task.Delay(_options.RetryDelay);
            }
        }
    }

    protected virtual bool ShouldRetry(Exception ex, int retryCount)
    {
        return retryCount < _options.MaxRetryAttempts && 
               (ex is HttpRequestException || ex is TimeoutException);
    }

    protected virtual async Task<T> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            
            var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result ?? throw new InvalidOperationException("Deserialization returned null");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing response. Status: {StatusCode}", response.StatusCode);
            throw;
        }
    }
}
