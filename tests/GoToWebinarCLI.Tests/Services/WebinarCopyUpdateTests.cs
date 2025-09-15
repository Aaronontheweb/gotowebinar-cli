using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;
using Xunit;

namespace GoToWebinarCLI.Tests.Services;

public class WebinarCopyUpdateTests
{
    private readonly GoToWebinarJsonContext _jsonContext;

    public WebinarCopyUpdateTests()
    {
        _jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    [Fact]
    public void UpdateWebinarRequest_ShouldSerializeOnlyNonNullFields()
    {
        // Arrange
        var request = new UpdateWebinarRequest
        {
            Subject = "Updated Subject",
            Description = null, // Should not be serialized
            Times = new List<WebinarTime>
            {
                new()
                {
                    StartTime = new DateTime(2025, 10, 15, 16, 0, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 15, 17, 0, 0, DateTimeKind.Utc)
                }
            },
            TimeZone = null // Should not be serialized
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonContext.UpdateWebinarRequest);
        var parsed = JsonDocument.Parse(json);

        // Assert
        Assert.True(parsed.RootElement.TryGetProperty("subject", out var subject));
        Assert.Equal("Updated Subject", subject.GetString());

        Assert.False(parsed.RootElement.TryGetProperty("description", out _));
        Assert.False(parsed.RootElement.TryGetProperty("timeZone", out _));

        Assert.True(parsed.RootElement.TryGetProperty("times", out var times));
        Assert.Equal(1, times.GetArrayLength());
    }

    [Fact]
    public void UpdateWebinarRequest_WithAllFields_ShouldSerializeCorrectly()
    {
        // Arrange
        var request = new UpdateWebinarRequest
        {
            Subject = "New Subject",
            Description = "New Description",
            Times = new List<WebinarTime>
            {
                new()
                {
                    StartTime = new DateTime(2025, 10, 15, 16, 0, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 15, 17, 30, 0, DateTimeKind.Utc)
                }
            },
            TimeZone = "America/Los_Angeles",
            Type = "single_session",
            IsPasswordProtected = true,
            ExperienceType = "CLASSIC"
        };

        // Act
        var json = JsonSerializer.Serialize(request, _jsonContext.UpdateWebinarRequest);
        var parsed = JsonDocument.Parse(json);

        // Assert
        Assert.Equal("New Subject", parsed.RootElement.GetProperty("subject").GetString());
        Assert.Equal("New Description", parsed.RootElement.GetProperty("description").GetString());
        Assert.Equal("America/Los_Angeles", parsed.RootElement.GetProperty("timeZone").GetString());
        Assert.Equal("single_session", parsed.RootElement.GetProperty("type").GetString());
        Assert.True(parsed.RootElement.GetProperty("isPasswordProtected").GetBoolean());
        Assert.Equal("CLASSIC", parsed.RootElement.GetProperty("experienceType").GetString());
    }

    [Fact]
    public void CreateWebinarRequest_ForCopy_ShouldHaveCorrectFields()
    {
        // Arrange
        var sourceWebinar = new Webinar
        {
            WebinarKey = "123456789",
            Subject = "Original Webinar",
            Description = "Original Description",
            TimeZone = "America/New_York",
            Times = new List<WebinarTime>
            {
                new()
                {
                    StartTime = new DateTime(2025, 9, 15, 16, 0, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 9, 15, 17, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        var newStartTime = new DateTime(2025, 10, 15, 16, 0, 0, DateTimeKind.Utc);
        var duration = 90; // minutes

        // Act - Simulate what the copy command does
        var request = new CreateWebinarRequest
        {
            Subject = sourceWebinar.Subject,
            Description = sourceWebinar.Description,
            TimeZone = sourceWebinar.TimeZone,
            Times = new List<WebinarTime>
            {
                new()
                {
                    StartTime = newStartTime,
                    EndTime = newStartTime.AddMinutes(duration)
                }
            }
        };

        // Assert
        Assert.Equal("Original Webinar", request.Subject);
        Assert.Equal("Original Description", request.Description);
        Assert.Equal("America/New_York", request.TimeZone);
        Assert.Single(request.Times);
        Assert.Equal(newStartTime, request.Times[0].StartTime);
        Assert.Equal(newStartTime.AddMinutes(90), request.Times[0].EndTime);
    }

    [Fact]
    public void WebinarTime_DurationCalculation_ShouldBeCorrect()
    {
        // Arrange
        var webinarTime = new WebinarTime
        {
            StartTime = new DateTime(2025, 10, 15, 16, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2025, 10, 15, 17, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var duration = (webinarTime.EndTime - webinarTime.StartTime).TotalMinutes;

        // Assert
        Assert.Equal(90, duration);
    }

    [Fact]
    public void UpdateWebinarRequest_EmptyRequest_ShouldNotHaveAnyFields()
    {
        // Arrange
        var request = new UpdateWebinarRequest();

        // Act
        var json = JsonSerializer.Serialize(request, _jsonContext.UpdateWebinarRequest);
        var parsed = JsonDocument.Parse(json);

        // Assert
        // An empty update request should serialize as {} with no fields
        var properties = parsed.RootElement.EnumerateObject().ToList();
        Assert.Empty(properties);
    }

    [Fact]
    public void CopyWebinar_WithOverrides_ShouldUseOverriddenValues()
    {
        // Arrange
        var sourceWebinar = new Webinar
        {
            Subject = "Original Subject",
            Description = "Original Description",
            TimeZone = "America/New_York"
        };

        var overrideSubject = "New Subject for October";
        var overrideDescription = "Updated description";
        var overrideTimeZone = "America/Los_Angeles";

        // Act - Simulate copy with overrides
        var request = new CreateWebinarRequest
        {
            Subject = overrideSubject ?? sourceWebinar.Subject,
            Description = overrideDescription ?? sourceWebinar.Description,
            TimeZone = overrideTimeZone ?? sourceWebinar.TimeZone,
            Times = new List<WebinarTime>
            {
                new()
                {
                    StartTime = new DateTime(2025, 10, 15, 16, 0, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 10, 15, 17, 0, 0, DateTimeKind.Utc)
                }
            }
        };

        // Assert
        Assert.Equal("New Subject for October", request.Subject);
        Assert.Equal("Updated description", request.Description);
        Assert.Equal("America/Los_Angeles", request.TimeZone);
    }

    [Fact]
    public void UpdateWebinar_PartialTimeUpdate_ShouldPreserveExistingDuration()
    {
        // Arrange
        var existingWebinar = new Webinar
        {
            Times = new List<WebinarTime>
            {
                new()
                {
                    StartTime = new DateTime(2025, 9, 15, 16, 0, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2025, 9, 15, 17, 30, 0, DateTimeKind.Utc) // 90 minutes
                }
            }
        };

        var newStartTime = new DateTime(2025, 10, 15, 14, 0, 0, DateTimeKind.Utc);
        var existingDuration = (existingWebinar.Times[0].EndTime - existingWebinar.Times[0].StartTime).TotalMinutes;

        // Act - Update only start time, preserve duration
        var updatedTime = new WebinarTime
        {
            StartTime = newStartTime,
            EndTime = newStartTime.AddMinutes(existingDuration)
        };

        // Assert
        Assert.Equal(new DateTime(2025, 10, 15, 14, 0, 0, DateTimeKind.Utc), updatedTime.StartTime);
        Assert.Equal(new DateTime(2025, 10, 15, 15, 30, 0, DateTimeKind.Utc), updatedTime.EndTime);
        Assert.Equal(90, (updatedTime.EndTime - updatedTime.StartTime).TotalMinutes);
    }
}