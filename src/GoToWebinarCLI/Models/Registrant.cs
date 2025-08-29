using System.Text.Json.Serialization;

namespace GoToWebinarCLI.Models;

public sealed class Registrant
{
    [JsonPropertyName("registrantKey")]
    public string RegistrantKey { get; set; } = string.Empty;

    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("registrationDate")]
    public DateTime RegistrationDate { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "APPROVED";

    [JsonPropertyName("joinUrl")]
    public string? JoinUrl { get; set; }

    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("questionsAndComments")]
    public string? QuestionsAndComments { get; set; }

    [JsonPropertyName("industry")]
    public string? Industry { get; set; }

    [JsonPropertyName("numberOfEmployees")]
    public string? NumberOfEmployees { get; set; }

    [JsonPropertyName("purchasingTimeFrame")]
    public string? PurchasingTimeFrame { get; set; }

    [JsonPropertyName("purchasingRole")]
    public string? PurchasingRole { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("responses")]
    public List<QuestionResponse>? Responses { get; set; }
}

public sealed class QuestionResponse
{
    [JsonPropertyName("questionKey")]
    public string QuestionKey { get; set; } = string.Empty;

    [JsonPropertyName("responseText")]
    public string ResponseText { get; set; } = string.Empty;
}

public sealed class CreateRegistrantRequest
{
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; set; }

    [JsonPropertyName("questionsAndComments")]
    public string? QuestionsAndComments { get; set; }

    [JsonPropertyName("industry")]
    public string? Industry { get; set; }

    [JsonPropertyName("numberOfEmployees")]
    public string? NumberOfEmployees { get; set; }

    [JsonPropertyName("purchasingTimeFrame")]
    public string? PurchasingTimeFrame { get; set; }

    [JsonPropertyName("purchasingRole")]
    public string? PurchasingRole { get; set; }

    [JsonPropertyName("responses")]
    public List<QuestionResponse>? Responses { get; set; }
}
