using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using GoToWebinarCLI.Models;
using Xunit;

namespace GoToWebinarCLI.Tests.Serialization;

public class JsonSerializationTests
{
    private readonly GoToWebinarJsonContext _jsonContext;
    private readonly JsonSerializerOptions _options;

    public JsonSerializationTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        _jsonContext = new GoToWebinarJsonContext(_options);
    }

    [Fact]
    public void Webinar_Serialization_RoundTrip()
    {
        // Arrange
        var webinar = new Webinar
        {
            WebinarKey = "test-key-123",
            WebinarId = "webinar-456",
            Subject = "Test Webinar Subject",
            Description = "Test webinar description with details",
            OrganizerKey = "organizer-789",
            Times = new List<WebinarTime>
            {
                new WebinarTime
                {
                    StartTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                    EndTime = new DateTime(2024, 1, 15, 11, 30, 0, DateTimeKind.Utc)
                }
            },
            RegistrationUrl = "https://example.com/register",
            InSession = false,
            Impromptu = false,
            TimeZone = "America/New_York"
        };

        // Act - Serialize
        var json = JsonSerializer.Serialize(webinar, _jsonContext.Webinar);

        // Act - Deserialize
        var deserialized = JsonSerializer.Deserialize(json, _jsonContext.Webinar);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.WebinarKey.Should().Be(webinar.WebinarKey);
        deserialized.WebinarId.Should().Be(webinar.WebinarId);
        deserialized.Subject.Should().Be(webinar.Subject);
        deserialized.Description.Should().Be(webinar.Description);
        deserialized.OrganizerKey.Should().Be(webinar.OrganizerKey);
        deserialized.Times.Should().HaveCount(1);
        deserialized.Times![0].StartTime.Should().Be(webinar.Times[0].StartTime);
        deserialized.Times[0].EndTime.Should().Be(webinar.Times[0].EndTime);
        deserialized.RegistrationUrl.Should().Be(webinar.RegistrationUrl);
        deserialized.InSession.Should().Be(webinar.InSession);
        deserialized.Impromptu.Should().Be(webinar.Impromptu);
        deserialized.TimeZone.Should().Be(webinar.TimeZone);
    }

    [Fact]
    public void PagedResponse_WithEmbeddedWebinars_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""_embedded"": {
                ""webinars"": [
                    {
                        ""webinarKey"": ""key1"",
                        ""webinarId"": ""id1"",
                        ""subject"": ""Webinar 1"",
                        ""organizerKey"": ""org1"",
                        ""times"": [
                            {
                                ""startTime"": ""2024-01-01T10:00:00Z"",
                                ""endTime"": ""2024-01-01T11:00:00Z""
                            }
                        ]
                    },
                    {
                        ""webinarKey"": ""key2"",
                        ""webinarId"": ""id2"",
                        ""subject"": ""Webinar 2"",
                        ""organizerKey"": ""org1""
                    }
                ]
            },
            ""page"": {
                ""size"": 10,
                ""totalElements"": 2,
                ""totalPages"": 1,
                ""number"": 0
            }
        }";

        // Act
        var result = JsonSerializer.Deserialize(json, _jsonContext.PagedResponseWebinar);

        // Assert
        result.Should().NotBeNull();
        result!.Page.Should().NotBeNull();
        result.Page!.Size.Should().Be(10);
        result.Page.TotalElements.Should().Be(2);
        result.Page.TotalPages.Should().Be(1);
        result.Page.Number.Should().Be(0);

        result.Embedded.Should().NotBeNull();
        result.Embedded!.Webinars.Should().NotBeNull();
        result.Embedded.Webinars!.Should().HaveCount(2);

        result.Embedded.Webinars![0].WebinarKey.Should().Be("key1");
        result.Embedded.Webinars[0].Subject.Should().Be("Webinar 1");
        result.Embedded.Webinars[0].Times.Should().HaveCount(1);

        result.Embedded.Webinars[1].WebinarKey.Should().Be("key2");
        result.Embedded.Webinars[1].Subject.Should().Be("Webinar 2");
    }

    [Fact]
    public void PagedResponse_EmptyResult_DeserializesCorrectly()
    {
        // Arrange
        var json = @"{
            ""page"": {
                ""size"": 10,
                ""totalElements"": 0,
                ""totalPages"": 0,
                ""number"": 0
            }
        }";

        // Act
        var result = JsonSerializer.Deserialize(json, _jsonContext.PagedResponseWebinar);

        // Assert
        result.Should().NotBeNull();
        result!.Page.Should().NotBeNull();
        result.Page!.Size.Should().Be(10);
        result.Page.TotalElements.Should().Be(0);
        result.Page.TotalPages.Should().Be(0);
        result.Page.Number.Should().Be(0);
        result.Embedded.Should().BeNull();
    }

    [Fact]
    public void Registrant_Serialization_RoundTrip()
    {
        // Arrange
        var registrant = new Registrant
        {
            RegistrantKey = "reg-123",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Status = "APPROVED",
            JoinUrl = "https://example.com/join",
            TimeZone = "UTC",
            Phone = "+1234567890",
            Organization = "Test Org",
            JobTitle = "Developer",
            QuestionsAndComments = "Test question",
            Industry = "Technology",
            NumberOfEmployees = "100-500",
            PurchasingTimeFrame = "Q1",
            PurchasingRole = "Decision Maker"
        };

        // Act
        var json = JsonSerializer.Serialize(registrant, _jsonContext.Registrant);
        var deserialized = JsonSerializer.Deserialize(json, _jsonContext.Registrant);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.RegistrantKey.Should().Be(registrant.RegistrantKey);
        deserialized.Email.Should().Be(registrant.Email);
        deserialized.FirstName.Should().Be(registrant.FirstName);
        deserialized.LastName.Should().Be(registrant.LastName);
        deserialized.Status.Should().Be(registrant.Status);
        deserialized.JoinUrl.Should().Be(registrant.JoinUrl);
        deserialized.TimeZone.Should().Be(registrant.TimeZone);
        deserialized.Phone.Should().Be(registrant.Phone);
        deserialized.Organization.Should().Be(registrant.Organization);
        deserialized.JobTitle.Should().Be(registrant.JobTitle);
        deserialized.QuestionsAndComments.Should().Be(registrant.QuestionsAndComments);
        deserialized.Industry.Should().Be(registrant.Industry);
        deserialized.NumberOfEmployees.Should().Be(registrant.NumberOfEmployees);
        deserialized.PurchasingTimeFrame.Should().Be(registrant.PurchasingTimeFrame);
        deserialized.PurchasingRole.Should().Be(registrant.PurchasingRole);
    }

    [Fact]
    public void OAuthToken_Serialization_HandlesNullValues()
    {
        // Arrange
        var token = new OAuthToken
        {
            AccessToken = "access-token-123",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            RefreshToken = "", // Empty value since it's not nullable
            OrganizerKey = "org-key",
            AccountKey = "", // Empty value since it's not nullable
            AccountType = "standard",
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com"
        };

        // Act
        var json = JsonSerializer.Serialize(token, _jsonContext.OAuthToken);
        var deserialized = JsonSerializer.Deserialize(json, _jsonContext.OAuthToken);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AccessToken.Should().Be(token.AccessToken);
        deserialized.TokenType.Should().Be(token.TokenType);
        deserialized.ExpiresIn.Should().Be(token.ExpiresIn);
        deserialized.RefreshToken.Should().BeEmpty();
        deserialized.OrganizerKey.Should().Be(token.OrganizerKey);
        deserialized.AccountKey.Should().BeEmpty();
        deserialized.AccountType.Should().Be(token.AccountType);
        deserialized.FirstName.Should().Be(token.FirstName);
        deserialized.LastName.Should().Be(token.LastName);
        deserialized.Email.Should().Be(token.Email);
    }

    [Fact]
    public void ErrorResponse_Deserialization_HandlesApiErrors()
    {
        // Arrange
        var json = @"{
            ""errorCode"": ""InvalidRequest"",
            ""description"": ""The request parameters are invalid"",
            ""message"": ""Bad Request"",
            ""incident"": ""incident-123-456""
        }";

        // Act
        var result = JsonSerializer.Deserialize(json, _jsonContext.ErrorResponse);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorCode.Should().Be("InvalidRequest");
        result.Description.Should().Be("The request parameters are invalid");
        result.Message.Should().Be("Bad Request");
        result.Incident.Should().Be("incident-123-456");
    }

    [Fact]
    public void ListWebinar_Serialization_HandlesCollection()
    {
        // Arrange
        var webinars = new List<Webinar>
        {
            new Webinar
            {
                WebinarKey = "key1",
                Subject = "Webinar 1",
                OrganizerKey = "org1"
            },
            new Webinar
            {
                WebinarKey = "key2",
                Subject = "Webinar 2",
                OrganizerKey = "org1"
            },
            new Webinar
            {
                WebinarKey = "key3",
                Subject = "Webinar 3",
                OrganizerKey = "org1"
            }
        };

        // Act
        var json = JsonSerializer.Serialize(webinars, _jsonContext.ListWebinar);
        var deserialized = JsonSerializer.Deserialize(json, _jsonContext.ListWebinar);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Should().HaveCount(3);
        deserialized![0].WebinarKey.Should().Be("key1");
        deserialized[0].Subject.Should().Be("Webinar 1");
        deserialized[1].WebinarKey.Should().Be("key2");
        deserialized[1].Subject.Should().Be("Webinar 2");
        deserialized[2].WebinarKey.Should().Be("key3");
        deserialized[2].Subject.Should().Be("Webinar 3");
    }

    [Fact]
    public void WebinarTime_DateTimeHandling_PreservesUtc()
    {
        // Arrange
        var utcTime = new DateTime(2024, 3, 15, 14, 30, 0, DateTimeKind.Utc);
        var webinarTime = new WebinarTime
        {
            StartTime = utcTime,
            EndTime = utcTime.AddHours(1.5)
        };

        // Act
        var json = JsonSerializer.Serialize(webinarTime, typeof(WebinarTime), _options);
        var deserialized = JsonSerializer.Deserialize<WebinarTime>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.StartTime.Should().Be(utcTime);
        deserialized.StartTime.Kind.Should().Be(DateTimeKind.Utc);
        deserialized.EndTime.Should().Be(utcTime.AddHours(1.5));

        // Verify ISO 8601 format in JSON
        json.Should().Contain("2024-03-15T14:30:00Z");
    }

    [Fact]
    public void ConfigProfile_SensitiveData_SerializesCorrectly()
    {
        // Arrange
        var profile = new ConfigProfile
        {
            ClientId = "client-id-123",
            ClientSecret = "secret-456",
            AccessToken = "token-789",
            RefreshToken = "refresh-abc",
            TokenExpiry = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc),
            OrganizerKey = "org-key",
            AccountKey = "account-key"
        };

        // Act
        var json = JsonSerializer.Serialize(profile, _jsonContext.ConfigProfile);
        var deserialized = JsonSerializer.Deserialize(json, _jsonContext.ConfigProfile);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ClientId.Should().Be(profile.ClientId);
        deserialized.ClientSecret.Should().Be(profile.ClientSecret);
        deserialized.AccessToken.Should().Be(profile.AccessToken);
        deserialized.RefreshToken.Should().Be(profile.RefreshToken);
        deserialized.TokenExpiry.Should().Be(profile.TokenExpiry);
        deserialized.OrganizerKey.Should().Be(profile.OrganizerKey);
        deserialized.AccountKey.Should().Be(profile.AccountKey);
    }
}
