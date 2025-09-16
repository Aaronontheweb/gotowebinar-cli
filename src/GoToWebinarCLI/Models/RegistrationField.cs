using System.Text.Json.Serialization;

namespace GoToWebinarCLI.Models;

public sealed class RegistrationFields
{
    [JsonPropertyName("fields")]
    public List<RegistrationField> Fields { get; set; } = new();

    [JsonPropertyName("questions")]
    public List<RegistrationQuestion> Questions { get; set; } = new();
}

public sealed class RegistrationField
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("answers")]
    public List<string>? Answers { get; set; }
}

public sealed class RegistrationQuestion
{
    [JsonPropertyName("questionKey")]
    public long QuestionKey { get; set; }

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "shortAnswer";

    [JsonPropertyName("answers")]
    public List<string>? Answers { get; set; }
}

public static class StandardRegistrationFields
{
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
    public const string Email = "email";
    public const string Organization = "organization";
    public const string JobTitle = "jobTitle";
    public const string QuestionsAndComments = "questionsAndComments";
    public const string Industry = "industry";
    public const string NumberOfEmployees = "numberOfEmployees";
    public const string PurchasingTimeFrame = "purchasingTimeFrame";
    public const string PurchasingRole = "purchasingRole";
    public const string Phone = "phone";
    public const string State = "state";
    public const string City = "city";
    public const string Country = "country";
    public const string ZipCode = "zipCode";
    public const string Address = "address";
}
