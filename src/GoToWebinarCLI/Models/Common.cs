using System.Text.Json.Serialization;

namespace GoToWebinarCLI.Models;

public sealed class ErrorResponse
{
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("incident")]
    public string? Incident { get; set; }
}

public sealed class PagedResponse<T>
{
    [JsonPropertyName("_embedded")]
    public EmbeddedData<T>? Embedded { get; set; }

    [JsonPropertyName("page")]
    public PageInfo? Page { get; set; }
}

public sealed class EmbeddedData<T>
{
    [JsonPropertyName("webinars")]
    public List<T>? Webinars { get; set; }

    [JsonPropertyName("registrants")]
    public List<T>? Registrants { get; set; }

    [JsonPropertyName("attendees")]
    public List<T>? Attendees { get; set; }
}

public sealed class PageInfo
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("totalElements")]
    public int TotalElements { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }
}
