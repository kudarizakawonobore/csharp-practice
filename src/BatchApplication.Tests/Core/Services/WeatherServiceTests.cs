using System;
using System.Threading;
using System.Threading.Tasks;
using BatchApplication.Core.Interfaces;
using BatchApplication.Core.Models;
using BatchApplication.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace BatchApplication.Tests.Core.Services;

public class WeatherServiceTests
{
    private readonly Mock<IWeatherApiClient> _mockWeatherApiClient;
    private readonly Mock<IWeatherRepository> _mockWeatherRepository;
    private readonly Mock<ILogger<WeatherService>> _mockLogger;
    private readonly WeatherService _weatherService;

    public WeatherServiceTests()
    {
        _mockWeatherApiClient = new Mock<IWeatherApiClient>();
        _mockWeatherRepository = new Mock<IWeatherRepository>();
        _mockLogger = new Mock<ILogger<WeatherService>>();

        _weatherService = new WeatherService(
            _mockWeatherApiClient.Object,
            _mockWeatherRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetWeatherData_ShouldReturnFromRepository_WhenDataIsRecent()
    {
        // Arrange
        var location = "Tokyo";
        var recentData = new WeatherData
        {
            Id = "1",
            Location = location,
            Temperature = 25.0,
            Humidity = 60.0,
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        _mockWeatherRepository
            .Setup(r => r.GetLatestByLocationAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentData);

        // Act
        var result = await _weatherService.GetWeatherDataAsync(location);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(recentData);
        
        // APIが呼ばれていないことを確認
        _mockWeatherApiClient.Verify(
            x => x.GetWeatherDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWeatherData_ShouldCallApi_WhenDataIsStale()
    {
        // Arrange
        var location = "Tokyo";
        var staleData = new WeatherData
        {
            Id = "1",
            Location = location,
            Temperature = 25.0,
            Humidity = 60.0,
            Timestamp = DateTime.UtcNow.AddHours(-2) // 2時間前のデータ
        };

        var newData = new WeatherData
        {
            Id = "2",
            Location = location,
            Temperature = 26.0,
            Humidity = 65.0,
            Timestamp = DateTime.UtcNow
        };

        _mockWeatherRepository
            .Setup(r => r.GetLatestByLocationAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleData);

        _mockWeatherApiClient
            .Setup(a => a.GetWeatherDataAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newData);

        // Act
        var result = await _weatherService.GetWeatherDataAsync(location);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(newData);
        
        // APIが呼ばれたことを確認
        _mockWeatherApiClient.Verify(
            x => x.GetWeatherDataAsync(location, It.IsAny<CancellationToken>()),
            Times.Once);
        
        // 新しいデータが保存されたことを確認
        _mockWeatherRepository.Verify(
            x => x.UpdateWeatherDataAsync(newData, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetWeatherData_ShouldHandleApiError_AndReturnStaleData()
    {
        // Arrange
        var location = "Tokyo";
        var staleData = new WeatherData
        {
            Id = "1",
            Location = location,
            Temperature = 25.0,
            Humidity = 60.0,
            Timestamp = DateTime.UtcNow.AddHours(-2)
        };

        _mockWeatherRepository
            .Setup(r => r.GetLatestByLocationAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleData);

        _mockWeatherApiClient
            .Setup(a => a.GetWeatherDataAsync(location, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _weatherService.GetWeatherDataAsync(location);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(staleData);
        
        // エラーがログされたことを確認
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetWeatherData_ShouldThrow_WhenNoDataExistsAndApiFails()
    {
        // Arrange
        var location = "Tokyo";

        _mockWeatherRepository
            .Setup(r => r.GetLatestByLocationAsync(location, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WeatherData?)null);

        _mockWeatherApiClient
            .Setup(a => a.GetWeatherDataAsync(location, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var action = () => _weatherService.GetWeatherDataAsync(location);

        // Assert
        await action.Should().ThrowAsync<Exception>()
            .WithMessage("Unable to get weather data for location: Tokyo");
    }
}
