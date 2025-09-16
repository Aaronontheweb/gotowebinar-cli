using System.Text.Json.Serialization;

namespace GoToWebinarCLI.Models;

public sealed class EmailSettings
{
    [JsonPropertyName("confirmationEmail")]
    public EmailConfiguration? ConfirmationEmail { get; set; }

    [JsonPropertyName("reminderEmails")]
    public List<ReminderEmailConfiguration>? ReminderEmails { get; set; }

    [JsonPropertyName("absenteeFollowUpEmail")]
    public EmailConfiguration? AbsenteeFollowUpEmail { get; set; }

    [JsonPropertyName("attendeeFollowUpEmail")]
    public AttendeeFollowUpEmailConfiguration? AttendeeFollowUpEmail { get; set; }
}

public class EmailConfiguration
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("includeCal")]
    public bool IncludeCalendar { get; set; } = true;
}

public sealed class ReminderEmailConfiguration : EmailConfiguration
{
    [JsonPropertyName("sendAtMinutesBefore")]
    public int SendAtMinutesBefore { get; set; }
}

public sealed class AttendeeFollowUpEmailConfiguration : EmailConfiguration
{
    [JsonPropertyName("includePoll")]
    public bool IncludePoll { get; set; }

    [JsonPropertyName("includeSurvey")]
    public bool IncludeSurvey { get; set; }

    [JsonPropertyName("includeRecording")]
    public bool IncludeRecording { get; set; }

    [JsonPropertyName("includeCertificate")]
    public bool IncludeCertificate { get; set; }
}

public sealed class BrandingTheme
{
    [JsonPropertyName("themeName")]
    public string ThemeName { get; set; } = string.Empty;

    [JsonPropertyName("logoUrl")]
    public string? LogoUrl { get; set; }

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("headerText")]
    public string? HeaderText { get; set; }

    [JsonPropertyName("footerText")]
    public string? FooterText { get; set; }

    [JsonPropertyName("footerHtml")]
    public string? FooterHtml { get; set; }
}

public sealed class WebinarSettings
{
    [JsonPropertyName("registrationUrl")]
    public string? RegistrationUrl { get; set; }

    [JsonPropertyName("approvalRequired")]
    public bool ApprovalRequired { get; set; }

    [JsonPropertyName("registrationLimit")]
    public int? RegistrationLimit { get; set; }

    [JsonPropertyName("isPasswordProtected")]
    public bool IsPasswordProtected { get; set; }

    [JsonPropertyName("webinarPassword")]
    public string? WebinarPassword { get; set; }

    [JsonPropertyName("isOndemand")]
    public bool IsOndemand { get; set; }

    [JsonPropertyName("experienceType")]
    public string ExperienceType { get; set; } = "CLASSIC";

    [JsonPropertyName("recordingAssetKey")]
    public string? RecordingAssetKey { get; set; }

    [JsonPropertyName("autopilot")]
    public bool Autopilot { get; set; }

    [JsonPropertyName("emailSettings")]
    public EmailSettings? EmailSettings { get; set; }

    [JsonPropertyName("brandingTheme")]
    public BrandingTheme? BrandingTheme { get; set; }
}

public sealed class RegistrationSettings
{
    [JsonPropertyName("approvalRequired")]
    public bool ApprovalRequired { get; set; }

    [JsonPropertyName("registrationLimit")]
    public int? RegistrationLimit { get; set; }

    [JsonPropertyName("allowRegistrationAfterStart")]
    public bool AllowRegistrationAfterStart { get; set; } = true;
}
