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

public sealed class OAuthToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("organizer_key")]
    public string? OrganizerKey { get; set; }

    [JsonPropertyName("account_key")]
    public string? AccountKey { get; set; }
}