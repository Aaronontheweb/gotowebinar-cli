using System.Text.Json.Serialization;

namespace GoToWebinarCLI.Models;

public sealed class ConfigFile
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("profiles")]
    public Dictionary<string, ConfigProfile> Profiles { get; set; } = new();

    [JsonPropertyName("currentProfile")]
    public string? CurrentProfile { get; set; }

    [JsonPropertyName("settings")]
    public ConfigSettings Settings { get; set; } = new();

    public ConfigProfile GetCurrentProfile()
    {
        if (string.IsNullOrEmpty(CurrentProfile))
        {
            CurrentProfile = "default";
            if (!Profiles.ContainsKey(CurrentProfile))
            {
                Profiles[CurrentProfile] = new ConfigProfile();
            }
        }

        if (!Profiles.TryGetValue(CurrentProfile, out var profile))
        {
            profile = new ConfigProfile();
            Profiles[CurrentProfile] = profile;
        }

        return profile;
    }
}

public sealed class ConfigProfile
{
    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    [JsonPropertyName("clientSecret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("tokenExpiry")]
    public DateTime? TokenExpiry { get; set; }

    [JsonPropertyName("organizerKey")]
    public string? OrganizerKey { get; set; }

    [JsonPropertyName("accountKey")]
    public string? AccountKey { get; set; }
}

public sealed class ConfigSettings
{
    [JsonPropertyName("defaultFormat")]
    public string DefaultFormat { get; set; } = "table";

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 100;

    [JsonPropertyName("autoUpdate")]
    public bool AutoUpdate { get; set; } = true;

    [JsonPropertyName("updateChannel")]
    public string UpdateChannel { get; set; } = "stable";
}

