using System;
using System.Collections.Generic;

namespace BatchApplication.Core.Common.ApiClient;

public class ApiClientOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
