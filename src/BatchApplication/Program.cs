using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BatchApplication.Core.Common.ApiClient;
using BatchApplication.Core.Common.Repository;
using BatchApplication.Core.Interfaces;
using BatchApplication.Infrastructure.ApiClients;
using BatchApplication.Infrastructure.Repositories;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // -v オプションが指定された場合、BaseUrlを出力して終了
        if (args.Contains("-v"))
        {
            var configuration = context.Configuration;
            var baseUrl = configuration.GetSection("ApiClient:BaseUrl").Value;
            Console.WriteLine($"API Base URL: {baseUrl}");
            Environment.Exit(0);
        }
        // Configuration
        services.Configure<ApiClientOptions>(context.Configuration.GetSection("ApiClient"));
        services.Configure<RepositoryOptions>(context.Configuration.GetSection("Repository"));

        // Database
        services.AddDbContext<DbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

        // HTTP Client
        services.AddHttpClient<IWeatherApiClient, WeatherApiClient>();

        // Repository
        services.AddScoped<IWeatherRepository, WeatherRepository>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        });
    })
    .Build();

try
{
    var weatherClient = host.Services.GetRequiredService<IWeatherApiClient>();
    var weatherRepository = host.Services.GetRequiredService<IWeatherRepository>();
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    var apiOptions = host.Services.GetRequiredService<IOptions<ApiClientOptions>>();

    // サンプル実行
    var locations = new[] { "Tokyo", "New York", "London" };
    foreach (var location in locations)
    {
        try
        {
            var weatherData = await weatherClient.GetWeatherDataAsync(location);
            await weatherRepository.UpdateWeatherDataAsync(weatherData);
            logger.LogInformation("Successfully updated weather data for {Location}", location);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing weather data for {Location}", location);
        }
    }
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Application error occurred");
    throw;
}
