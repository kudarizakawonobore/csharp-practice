using System;

namespace BatchApplication.Core.Common.Repository;

public class RepositoryOptions
{
    public int CommandTimeout { get; set; } = 30;
    public bool EnableDetailedLogging { get; set; } = false;
    public RetryOptions RetryOptions { get; set; } = new();
}

public class RetryOptions
{
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
}
