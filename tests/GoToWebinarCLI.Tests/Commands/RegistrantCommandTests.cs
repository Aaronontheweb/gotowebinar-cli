using FluentAssertions;
using GoToWebinarCLI.Commands;
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
        var registrant = TestDataBuilder.CreateRegistrant(1234567890123L);
        _mockApiClient.Setup(x => x.GetRegistrantAsync(
                _testWebinarKey,
                "1234567890123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(registrant);

        // Act
        var result = await _mockApiClient.Object.GetRegistrantAsync(_testWebinarKey, "1234567890123");

        // Assert
        result.Should().NotBeNull();
        result!.RegistrantKey.Should().Be(1234567890123L);
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
            RegistrantKey = 4567890123456789L,
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

    [Theory]
    [InlineData("waiting")]
    [InlineData("approved")]
    [InlineData("denied")]
    [InlineData("WAITING")]
    [InlineData("Approved")]
    [InlineData("DENIED")]
    public void IsValidStatus_WithCanonicalStatus_ShouldReturnTrue(string status)
    {
        RegistrantCommand.IsValidStatus(status).Should().BeTrue();
    }

    [Theory]
    [InlineData("pending")]    // the classic mistake: API uses WAITING, not PENDING
    [InlineData("PENDING")]
    [InlineData("active")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValidStatus_WithUnknownOrEmptyStatus_ShouldReturnFalse(string? status)
    {
        RegistrantCommand.IsValidStatus(status).Should().BeFalse();
    }

    [Fact]
    public void ValidStatuses_ShouldNotContainPending()
    {
        // Guards against re-introducing the wrong value: "pending" is not a real
        // GoToWebinar registrant status — unapproved registrants are "waiting".
        RegistrantCommand.ValidStatuses.Should().BeEquivalentTo(new[] { "waiting", "approved", "denied" });
        RegistrantCommand.ValidStatuses.Should().NotContain("pending");
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

    [Fact]
    public void FormatRegistrantsJson_WithEmptyList_ShouldReturnEmptyJsonArray()
    {
        // An empty webinar must serialize to a valid empty JSON array, not a human
        // sentence — otherwise --format json emits non-JSON and breaks consumers.
        var json = RegistrantCommand.FormatRegistrantsJson(new List<Registrant>());

        json.Trim().Should().Be("[]");
    }

    [Fact]
    public void FormatRegistrantsJson_WithNull_ShouldReturnEmptyJsonArray()
    {
        // A null result (no registrants endpoint payload) must be treated as empty,
        // never throw, and still produce valid JSON.
        var json = RegistrantCommand.FormatRegistrantsJson(null);

        json.Trim().Should().Be("[]");
    }

    [Fact]
    public void FormatRegistrantsJson_WithRegistrants_ShouldReturnPopulatedJsonArray()
    {
        var registrants = TestDataBuilder.CreateRegistrants(3);

        var json = RegistrantCommand.FormatRegistrantsJson(registrants);

        json.Trim().Should().StartWith("[");
        json.Trim().Should().EndWith("]");
        json.Should().Contain("john.doe1@example.com");
    }

    [Fact]
    public void FormatRegistrantsCsv_WithEmptyList_ShouldReturnHeaderOnly()
    {
        // An empty webinar must emit the CSV header row only (no data rows), not a
        // human sentence — so --format csv stays parseable.
        var csv = RegistrantCommand.FormatRegistrantsCsv(new List<Registrant>());

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        lines.Should().ContainSingle();
        lines[0].Should().Be(RegistrantCommand.CsvHeader);
    }

    [Fact]
    public void FormatRegistrantsCsv_WithNull_ShouldReturnHeaderOnly()
    {
        // A null result must be treated as empty, never throw, and produce header-only CSV.
        var csv = RegistrantCommand.FormatRegistrantsCsv(null);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        lines.Should().ContainSingle();
        lines[0].Should().Be(RegistrantCommand.CsvHeader);
    }

    [Fact]
    public void FormatRegistrantsCsv_WithRegistrants_ShouldReturnHeaderPlusDataRows()
    {
        var registrants = TestDataBuilder.CreateRegistrants(3);

        var csv = RegistrantCommand.FormatRegistrantsCsv(registrants);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        lines[0].Should().Be(RegistrantCommand.CsvHeader);
        lines.Should().HaveCount(4); // header + 3 data rows
    }

    [Fact]
    public void CsvHeader_ShouldMatchExpectedColumns()
    {
        RegistrantCommand.CsvHeader.Should().Be(
            "RegistrantKey,FirstName,LastName,Email,Status,RegisteredAt,JoinUrl");
    }
}

