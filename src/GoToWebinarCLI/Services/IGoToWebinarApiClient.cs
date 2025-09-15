using GoToWebinarCLI.Models;

namespace GoToWebinarCLI.Services;

public interface IGoToWebinarApiClient : IDisposable
{
    Task<List<Webinar>?> GetWebinarsAsync(
        bool upcoming = true,
        DateTime? fromTime = null,
        DateTime? toTime = null,
        CancellationToken cancellationToken = default);

    Task<Webinar?> GetWebinarAsync(string webinarKey, CancellationToken cancellationToken = default);

    Task<Webinar?> CreateWebinarAsync(CreateWebinarRequest request, CancellationToken cancellationToken = default);

    Task<Webinar?> UpdateWebinarAsync(string webinarKey, UpdateWebinarRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteWebinarAsync(string webinarKey, CancellationToken cancellationToken = default);

    Task<List<Registrant>?> GetRegistrantsAsync(
        string webinarKey,
        CancellationToken cancellationToken = default);

    Task<Registrant?> GetRegistrantAsync(
        string webinarKey,
        string registrantKey,
        CancellationToken cancellationToken = default);

    Task<Registrant?> AddRegistrantAsync(
        string webinarKey,
        CreateRegistrantRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveRegistrantAsync(
        string webinarKey,
        string registrantKey,
        CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

