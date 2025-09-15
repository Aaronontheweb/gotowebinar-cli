using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;
using Moq;
using Moq.Protected;
using Xunit;

namespace GoToWebinarCLI.Tests.Services;

public class GoToWebinarApiClientUpdateTests : IDisposable
{
    private readonly GoToWebinarJsonContext _jsonContext;
    private readonly string _tempConfigPath;
    private readonly ConfigurationService _configService;
    private readonly ConfigFile _testConfig;

    public GoToWebinarApiClientUpdateTests()
    {
        _jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        // Create a temp config file for testing
        _tempConfigPath = Path.Combine(Path.GetTempPath(), $"test_config_{Guid.NewGuid()}.json");

        _testConfig = new ConfigFile
        {
            CurrentProfile = "test",
            Profiles = new Dictionary<string, ConfigProfile>
            {
                ["test"] = new ConfigProfile
                {
                    ClientId = "test-client-id",
                    ClientSecret = "test-client-secret",
                    AccessToken = "test-access-token",
                    RefreshToken = "test-refresh-token",
                    TokenExpiry = DateTime.UtcNow.AddHours(1),
                    OrganizerKey = "test-organizer-key",
                    AccountKey = "test-account-key"
                }
            }
        };

        // Write test config to temp file
        File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(_testConfig, _jsonContext.ConfigFile));

        // Set environment variable to use test config
        Environment.SetEnvironmentVariable("GOTOWEBINAR_CONFIG_PATH", _tempConfigPath);

        _configService = new ConfigurationService();
    }

    public void Dispose()
    {
        // Clean up
        Environment.SetEnvironmentVariable("GOTOWEBINAR_CONFIG_PATH", null);
        if (File.Exists(_tempConfigPath))
        {
            File.Delete(_tempConfigPath);
        }
    }

    [Fact]
    public async Task UpdateWebinarAsync_Success_ReturnsUpdatedWebinar()
    {
        // Arrange
        var webinarKey = "123456789";
        var updateRequest = new UpdateWebinarRequest
        {
            Subject = "Updated Subject",
            Description = "Updated Description"
        };

        var updatedWebinar = new Webinar
        {
            WebinarKey = webinarKey,
            Subject = "Updated Subject",
            Description = "Updated Description",
            TimeZone = "America/New_York"
        };

        var mockHandler = new Mock<HttpMessageHandler>();

        // Setup PUT request response
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains($"/webinars/{webinarKey}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            });

        // Setup GET request response (for fetching updated webinar)
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains($"/webinars/{webinarKey}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(updatedWebinar, _jsonContext.Webinar),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.getgo.com/G2W/rest/v2/")
        };

        var apiClient = new TestableGoToWebinarApiClient(httpClient, _configService);

        // Act
        var result = await apiClient.UpdateWebinarAsync(webinarKey, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(webinarKey, result!.WebinarKey);
        Assert.Equal("Updated Subject", result.Subject);
        Assert.Equal("Updated Description", result.Description);
    }

    [Fact]
    public async Task UpdateWebinarAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var webinarKey = "nonexistent";
        var updateRequest = new UpdateWebinarRequest
        {
            Subject = "Updated Subject"
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(
                    JsonSerializer.Serialize(new ErrorResponse
                    {
                        ErrorCode = "NotFound",
                        Description = "Webinar not found"
                    }, _jsonContext.ErrorResponse),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.getgo.com/G2W/rest/v2/")
        };

        var apiClient = new TestableGoToWebinarApiClient(httpClient, _configService);

        // Act
        var result = await apiClient.UpdateWebinarAsync(webinarKey, updateRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateWebinarAsync_WithTimes_UpdatesCorrectly()
    {
        // Arrange
        var webinarKey = "123456789";
        var newStartTime = new DateTime(2025, 10, 15, 16, 0, 0, DateTimeKind.Utc);
        var updateRequest = new UpdateWebinarRequest
        {
            Times = new List<WebinarTime>
            {
                new()
                {
                    StartTime = newStartTime,
                    EndTime = newStartTime.AddMinutes(90)
                }
            }
        };

        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Put &&
                    req.RequestUri != null &&
                    req.RequestUri.ToString().Contains($"/webinars/{webinarKey}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NoContent
            })
            .Callback<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                if (request.Content != null)
                {
                    var content = await request.Content.ReadAsStringAsync();
                    var parsed = JsonDocument.Parse(content);

                    // Verify the request contains the times
                    Assert.True(parsed.RootElement.TryGetProperty("times", out var times));
                    Assert.Equal(1, times.GetArrayLength());
                }
            });

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(new Webinar
                    {
                        WebinarKey = webinarKey,
                        Times = updateRequest.Times
                    }, _jsonContext.Webinar),
                    Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.getgo.com/G2W/rest/v2/")
        };

        var apiClient = new TestableGoToWebinarApiClient(httpClient, _configService);

        // Act
        var result = await apiClient.UpdateWebinarAsync(webinarKey, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result!.Times);
        Assert.Single(result.Times!);
        Assert.Equal(newStartTime, result.Times![0].StartTime);
    }

    // Testable version of GoToWebinarApiClient that allows injection of HttpClient
    private class TestableGoToWebinarApiClient : GoToWebinarApiClient
    {
        public TestableGoToWebinarApiClient(HttpClient httpClient, ConfigurationService configService)
            : base(configService)
        {
            // Use reflection to set the private _httpClient field
            var field = typeof(GoToWebinarApiClient).GetField("_httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, httpClient);
        }
    }
}
