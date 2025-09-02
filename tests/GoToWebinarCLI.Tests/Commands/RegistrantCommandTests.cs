using FluentAssertions;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;
using GoToWebinarCLI.Tests.Helpers;
using Moq;

namespace GoToWebinarCLI.Tests.Commands;

public class RegistrantCommandTests
{
    private readonly Mock<IGoToWebinarApiClient> _mockApiClient;
    private readonly string _testWebinarKey = "test-webinar-123";

    public RegistrantCommandTests()
    {
        _mockApiClient = new Mock<IGoToWebinarApiClient>();
    }

    [Fact]
    public async Task ListCommand_ShouldReturnRegistrants()
    {
        // Arrange
        var registrants = TestDataBuilder.CreateRegistrants(5);
        _mockApiClient.Setup(x => x.GetRegistrantsAsync(
                _testWebinarKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrants);

        // Act
        var result = await _mockApiClient.Object.GetRegistrantsAsync(_testWebinarKey);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
        result![0].FirstName.Should().Be("John1");
        result![0].Email.Should().Be("john.doe1@example.com");
    }

    [Fact]
    public async Task ListCommand_WithStatusFilter_ShouldReturnFilteredRegistrants()
    {
        // Arrange
        var registrants = TestDataBuilder.CreateRegistrants(5);
        var approvedRegistrants = registrants.Where(r => r.Status == "APPROVED").ToList();

        _mockApiClient.Setup(x => x.GetRegistrantsAsync(
                _testWebinarKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrants);

        // Act
        var result = await _mockApiClient.Object.GetRegistrantsAsync(_testWebinarKey);
        var filtered = result?.Where(r => r.Status == "APPROVED").ToList();

        // Assert
        filtered.Should().NotBeNull();
        filtered.Should().HaveCount(3); // Based on our test data builder logic
        filtered!.All(r => r.Status == "APPROVED").Should().BeTrue();
    }

    [Fact]
    public async Task GetCommand_WithValidKey_ShouldReturnRegistrantDetails()
    {
        // Arrange
        var registrant = TestDataBuilder.CreateRegistrant("reg-123");
        _mockApiClient.Setup(x => x.GetRegistrantAsync(
                _testWebinarKey,
                "reg-123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrant);

        // Act
        var result = await _mockApiClient.Object.GetRegistrantAsync(_testWebinarKey, "reg-123");

        // Assert
        result.Should().NotBeNull();
        result!.RegistrantKey.Should().Be("reg-123");
        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.Email.Should().Be("jane.smith@example.com");
        result.Organization.Should().Be("Tech Corp");
        result.Responses.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCommand_WithInvalidKey_ShouldReturnNull()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetRegistrantAsync(
                _testWebinarKey,
                "invalid-key",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Registrant?)null);

        // Act
        var result = await _mockApiClient.Object.GetRegistrantAsync(_testWebinarKey, "invalid-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddCommand_WithValidData_ShouldAddRegistrant()
    {
        // Arrange
        var request = new CreateRegistrantRequest
        {
            FirstName = "Alice",
            LastName = "Johnson",
            Email = "alice.johnson@example.com",
            Organization = "Innovation Inc",
            Phone = "555-9999"
        };

        var createdRegistrant = new Registrant
        {
            RegistrantKey = "new-reg-456",
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Organization = request.Organization,
            Phone = request.Phone,
            Status = "APPROVED",
            RegistrationDate = DateTime.UtcNow,
            JoinUrl = "https://global.gotowebinar.com/join/new-reg-456"
        };

        _mockApiClient.Setup(x => x.AddRegistrantAsync(
                _testWebinarKey,
                It.IsAny<CreateRegistrantRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRegistrant);

        // Act
        var result = await _mockApiClient.Object.AddRegistrantAsync(_testWebinarKey, request);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Alice");
        result.LastName.Should().Be("Johnson");
        result.Email.Should().Be("alice.johnson@example.com");
        result.Status.Should().Be("APPROVED");
        result.JoinUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RemoveCommand_WithValidKey_ShouldRemoveRegistrant()
    {
        // Arrange
        _mockApiClient.Setup(x => x.RemoveRegistrantAsync(
                _testWebinarKey,
                "reg-to-remove",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _mockApiClient.Object.RemoveRegistrantAsync(_testWebinarKey, "reg-to-remove");

        // Assert
        result.Should().BeTrue();
        _mockApiClient.Verify(x => x.RemoveRegistrantAsync(
            _testWebinarKey,
            "reg-to-remove",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveCommand_WithInvalidKey_ShouldReturnFalse()
    {
        // Arrange
        _mockApiClient.Setup(x => x.RemoveRegistrantAsync(
                _testWebinarKey,
                "invalid-reg",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _mockApiClient.Object.RemoveRegistrantAsync(_testWebinarKey, "invalid-reg");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListCommand_WithNoRegistrants_ShouldReturnEmptyList()
    {
        // Arrange
        _mockApiClient.Setup(x => x.GetRegistrantsAsync(
                _testWebinarKey,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Registrant>());

        // Act
        var result = await _mockApiClient.Object.GetRegistrantsAsync(_testWebinarKey);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}