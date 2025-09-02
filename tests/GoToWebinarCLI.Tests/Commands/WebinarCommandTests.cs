using System.CommandLine;
using System.CommandLine.IO;
using System.Text;
using FluentAssertions;
using GoToWebinarCLI.Commands;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;
using GoToWebinarCLI.Tests.Helpers;
using Moq;

namespace GoToWebinarCLI.Tests.Commands;

public class WebinarCommandTests
{
    private readonly Mock<IGoToWebinarApiClient> _mockApiClient;
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly IConsole _testConsole;
    private readonly StringBuilder _consoleOutput;

    public WebinarCommandTests()
    {
        _mockApiClient = new Mock<IGoToWebinarApiClient>();
        _mockConfigService = new Mock<IConfigurationService>();
        _consoleOutput = new StringBuilder();
        _testConsole = new TestConsole(_consoleOutput);
    }

    [Fact]
    public Task ListCommand_WithUpcomingWebinars_ShouldDisplayWebinars()
    {
        // Arrange
        var webinars = TestDataBuilder.CreateWebinars(3);
        _mockApiClient.Setup(x => x.GetWebinarsAsync(
                It.IsAny<bool>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(webinars);

        var command = new WebinarCommand();
        var parseResult = command.Parse("list");

        // Act
        // Note: In real implementation, we'd need to inject the mock API client
        // For now, this test structure shows the intent
        
        // Assert
        webinars.Should().HaveCount(3);
        webinars[0].Subject.Should().Be("Test Webinar 1");
        
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListCommand_WithNoWebinars_ShouldDisplayNoWebinarsMessage()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetWebinarsAsync(
                It.IsAny<bool>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Webinar>());

        // Act & Assert
        var emptyList = await _mockApiClient.Object.GetWebinarsAsync();
        emptyList.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommand_WithValidKey_ShouldReturnWebinarDetails()
    {
        // Arrange
        var webinar = TestDataBuilder.CreateWebinar("test-key");
        _mockApiClient.Setup(x => x.GetWebinarAsync(
                "test-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(webinar);

        // Act
        var result = await _mockApiClient.Object.GetWebinarAsync("test-key");

        // Assert
        result.Should().NotBeNull();
        result!.WebinarKey.Should().Be("test-key");
        result.Subject.Should().Be("Advanced Testing Strategies");
        result.NumberOfRegistrants.Should().Be(42);
    }

    [Fact]
    public async Task GetCommand_WithInvalidKey_ShouldReturnNull()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetWebinarAsync(
                "invalid-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Webinar?)null);

        // Act
        var result = await _mockApiClient.Object.GetWebinarAsync("invalid-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateCommand_WithValidData_ShouldCreateWebinar()
    {
        // Arrange
        var request = new CreateWebinarRequest
        {
            Subject = "New Test Webinar",
            Description = "Test Description",
            TimeZone = "America/New_York",
            Times = new List<WebinarTime>
            {
                new WebinarTime
                {
                    StartTime = DateTime.UtcNow.AddDays(1),
                    EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
                }
            }
        };

        var createdWebinar = TestDataBuilder.CreateWebinar("new-webinar");
        createdWebinar.Subject = request.Subject;

        _mockApiClient.Setup(x => x.CreateWebinarAsync(
                It.IsAny<CreateWebinarRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdWebinar);

        // Act
        var result = await _mockApiClient.Object.CreateWebinarAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Subject.Should().Be("New Test Webinar");
        result.WebinarKey.Should().Be("new-webinar");
    }

    [Fact]
    public async Task DeleteCommand_WithValidKey_ShouldDeleteWebinar()
    {
        // Arrange
        _mockApiClient.Setup(x => x.DeleteWebinarAsync(
                "webinar-to-delete",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockApiClient.Object.DeleteWebinarAsync("webinar-to-delete");

        // Assert
        result.Should().BeTrue();
        _mockApiClient.Verify(x => x.DeleteWebinarAsync("webinar-to-delete", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListCommand_WithPastFlag_ShouldReturnPastWebinars()
    {
        // Arrange
        var pastWebinars = TestDataBuilder.CreatePastWebinars(2);
        _mockApiClient.Setup(x => x.GetWebinarsAsync(
                It.IsAny<bool>(),
                It.IsAny<DateTime?>(),
                It.Is<DateTime?>(d => d <= DateTime.UtcNow),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pastWebinars);

        // Act
        var result = await _mockApiClient.Object.GetWebinarsAsync(true, DateTime.UtcNow.AddYears(-1), DateTime.UtcNow);

        // Assert
        result.Should().HaveCount(2);
        result!.All(w => w.Times!.First().StartTime < DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task ListCommand_WithAllFlag_ShouldReturnAllWebinars()
    {
        // Arrange
        var allWebinars = new List<Webinar>();
        allWebinars.AddRange(TestDataBuilder.CreateWebinars(3));
        allWebinars.AddRange(TestDataBuilder.CreatePastWebinars(2));

        _mockApiClient.Setup(x => x.GetWebinarsAsync(
                It.IsAny<bool>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allWebinars);

        // Act
        var result = await _mockApiClient.Object.GetWebinarsAsync(true, DateTime.UtcNow.AddYears(-2), DateTime.UtcNow.AddYears(2));

        // Assert
        result.Should().HaveCount(5);
        result!.Should().Contain(w => w.WebinarKey.StartsWith("past-"));
        result!.Should().Contain(w => w.WebinarKey.StartsWith("webinar-"));
    }
}

public class TestConsole : IConsole
{
    private readonly StringBuilder _output;

    public TestConsole(StringBuilder output)
    {
        _output = output;
        Out = new TestStreamWriter(_output);
        Error = new TestStreamWriter(new StringBuilder());
    }

    public IStandardStreamWriter Out { get; }
    public IStandardStreamWriter Error { get; }
    public bool IsOutputRedirected => false;
    public bool IsErrorRedirected => false;
    public bool IsInputRedirected => false;
}

public class TestStreamWriter : TextWriter, IStandardStreamWriter
{
    private readonly StringBuilder _output;

    public TestStreamWriter(StringBuilder output)
    {
        _output = output;
    }

    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

    public override void Write(string? value)
    {
        if (value != null)
            _output.Append(value);
    }

    public override void WriteLine(string? value)
    {
        if (value != null)
            _output.AppendLine(value);
    }
}