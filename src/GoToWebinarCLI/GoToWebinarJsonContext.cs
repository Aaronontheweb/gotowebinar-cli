using System.Text.Json.Serialization;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;

namespace GoToWebinarCLI;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ConfigFile))]
[JsonSerializable(typeof(ConfigProfile))]
[JsonSerializable(typeof(ConfigSettings))]
[JsonSerializable(typeof(OAuthToken))]
[JsonSerializable(typeof(OAuthErrorResponse))]
[JsonSerializable(typeof(AuthorizationCodeRequest))]
[JsonSerializable(typeof(RefreshTokenRequest))]
[JsonSerializable(typeof(Webinar))]
[JsonSerializable(typeof(Webinar[]))]
[JsonSerializable(typeof(List<Webinar>))]
[JsonSerializable(typeof(WebinarTime))]
[JsonSerializable(typeof(CreateWebinarRequest))]
[JsonSerializable(typeof(UpdateWebinarRequest))]
[JsonSerializable(typeof(Registrant))]
[JsonSerializable(typeof(Registrant[]))]
[JsonSerializable(typeof(List<Registrant>))]
[JsonSerializable(typeof(CreateRegistrantRequest))]
[JsonSerializable(typeof(QuestionResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(RegistrationFields))]
[JsonSerializable(typeof(RegistrationField))]
[JsonSerializable(typeof(RegistrationQuestion))]
[JsonSerializable(typeof(List<RegistrationField>))]
[JsonSerializable(typeof(List<RegistrationQuestion>))]
[JsonSerializable(typeof(EmailSettings))]
[JsonSerializable(typeof(EmailConfiguration))]
[JsonSerializable(typeof(ReminderEmailConfiguration))]
[JsonSerializable(typeof(AttendeeFollowUpEmailConfiguration))]
[JsonSerializable(typeof(BrandingTheme))]
[JsonSerializable(typeof(WebinarSettings))]
[JsonSerializable(typeof(List<ReminderEmailConfiguration>))]
[JsonSerializable(typeof(PagedResponse<Webinar>), TypeInfoPropertyName = "PagedResponseWebinar")]
[JsonSerializable(typeof(PagedResponse<Registrant>), TypeInfoPropertyName = "PagedResponseRegistrant")]
[JsonSerializable(typeof(Dictionary<string, ConfigProfile>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<Webinar>), TypeInfoPropertyName = "ListWebinar")]
[JsonSerializable(typeof(List<Registrant>), TypeInfoPropertyName = "ListRegistrant")]
[JsonSerializable(typeof(UpdateInfo))]
public partial class GoToWebinarJsonContext : JsonSerializerContext
{
}
