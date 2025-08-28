using System.Text.Json.Serialization;

namespace GoToWebinarCLI.Models;

public sealed class Webinar
{
    [JsonPropertyName("webinarKey")]
    public string WebinarKey { get; set; } = string.Empty;

    [JsonPropertyName("webinarID")]
    public string? WebinarId { get; set; }

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("times")]
    public List<WebinarTime>? Times { get; set; }

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = "America/New_York";

    [JsonPropertyName("registrationUrl")]
    public string? RegistrationUrl { get; set; }

    [JsonPropertyName("inSession")]
    public bool InSession { get; set; }

    [JsonPropertyName("impromptu")]
    public bool Impromptu { get; set; }

    [JsonPropertyName("organizerKey")]
    public string? OrganizerKey { get; set; }

    [JsonPropertyName("accountKey")]
    public string? AccountKey { get; set; }

    [JsonPropertyName("registrationLimit")]
    public int? RegistrationLimit { get; set; }

    [JsonPropertyName("numberOfRegistrants")]
    public int? NumberOfRegistrants { get; set; }
}

public sealed class WebinarTime
{
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }
}

public sealed class CreateWebinarRequest
{
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("times")]
    public List<WebinarTime> Times { get; set; } = new();

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = "America/New_York";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "single_session";

    [JsonPropertyName("isPasswordProtected")]
    public bool IsPasswordProtected { get; set; }

    [JsonPropertyName("recordingAssetKey")]
    public string? RecordingAssetKey { get; set; }

    [JsonPropertyName("isOndemand")]
    public bool IsOndemand { get; set; }

    [JsonPropertyName("experienceType")]
    public string ExperienceType { get; set; } = "CLASSIC";
}