using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoToWebinarCLI.Models;

namespace GoToWebinarCLI.Services;

public class GoToWebinarApiClient : IGoToWebinarApiClient
{
    private const string BaseUrl = "https://api.getgo.com/G2W/rest/v2/";
    private readonly HttpClient _httpClient;
    private readonly ConfigurationService _configService;
    private readonly AuthenticationService _authService;
    private readonly GoToWebinarJsonContext _jsonContext;
    private readonly Dictionary<string, (DateTime Expiry, string Data)> _cache = new();
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public GoToWebinarApiClient(ConfigurationService configService)
    {
        _configService = configService;
        _jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions
        {
            WriteIndented = false
        });

        var rateLimitHandler = new RateLimitHandler
        {
            InnerHandler = new HttpClientHandler()
        };

        _httpClient = new HttpClient(rateLimitHandler)
        {
            BaseAddress = new Uri(BaseUrl)
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GoToWebinar-CLI/1.0");

        _authService = new AuthenticationService(_httpClient, configService);
    }

    private async Task<bool> EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        if (string.IsNullOrEmpty(profile.AccessToken))
        {
            Console.WriteLine("Error: Not authenticated. Run 'gotowebinar config auth' first.");
            return false;
        }

        if (profile.TokenExpiry <= DateTime.UtcNow)
        {
            var refreshed = await _authService.RefreshTokenAsync(cancellationToken);
            if (!refreshed)
            {
                Console.WriteLine("Error: Failed to refresh token. Please re-authenticate.");
                return false;
            }

            config = await _configService.GetConfigAsync();
            profile = config.GetCurrentProfile();
        }

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", profile.AccessToken);

        return true;
    }

    public async Task<List<Webinar>?> GetWebinarsAsync(
        bool upcoming = true,
        DateTime? fromTime = null,
        DateTime? toTime = null,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var from = fromTime ?? DateTime.UtcNow;
        var to = toTime ?? DateTime.UtcNow.AddMonths(12);

        var url = $"organizers/{profile.OrganizerKey}/webinars" +
                  $"?fromTime={from:yyyy-MM-ddTHH:mm:ssZ}" +
                  $"&toTime={to:yyyy-MM-ddTHH:mm:ssZ}";

        var cacheKey = $"webinars_{upcoming}_{from:yyyyMMdd}_{to:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
        {
            return JsonSerializer.Deserialize(cached.Data, _jsonContext.ListWebinar);
        }

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // Check if response contains _embedded structure
            List<Webinar>? webinars;
            if (content.Contains("\"_embedded\""))
            {
                var pagedResponse = JsonSerializer.Deserialize(content, _jsonContext.PagedResponseWebinar);
                webinars = pagedResponse?.Embedded?.Webinars;
            }
            else
            {
                // Empty response just has page info
                webinars = new List<Webinar>();
            }

            _cache[cacheKey] = (DateTime.UtcNow.Add(_cacheExpiry), content);

            return webinars;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to get webinars - {ex.Message}");
            return null;
        }
    }

    public async Task<Webinar?> GetWebinarAsync(string webinarKey, CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}";
        var cacheKey = $"webinar_{webinarKey}";

        if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
        {
            return JsonSerializer.Deserialize(cached.Data, _jsonContext.Webinar);
        }

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var webinar = JsonSerializer.Deserialize(content, _jsonContext.Webinar);

            _cache[cacheKey] = (DateTime.UtcNow.Add(_cacheExpiry), content);

            return webinar;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to get webinar - {ex.Message}");
            return null;
        }
    }

    public async Task<Webinar?> CreateWebinarAsync(CreateWebinarRequest request, CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars";

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonContext.CreateWebinarRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var webinar = JsonSerializer.Deserialize(responseContent, _jsonContext.Webinar);

            ClearCache();

            return webinar;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to create webinar - {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteWebinarAsync(string webinarKey, CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return false;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}";

        try
        {
            var response = await _httpClient.DeleteAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return false;
            }

            ClearCache();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to delete webinar - {ex.Message}");
            return false;
        }
    }

    public async Task<List<Registrant>?> GetRegistrantsAsync(
        string webinarKey,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}/registrants";
        var cacheKey = $"registrants_{webinarKey}";

        if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
        {
            return JsonSerializer.Deserialize(cached.Data, _jsonContext.ListRegistrant);
        }

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var registrants = JsonSerializer.Deserialize(content, _jsonContext.ListRegistrant);

            _cache[cacheKey] = (DateTime.UtcNow.Add(_cacheExpiry), content);

            return registrants;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to get registrants - {ex.Message}");
            return null;
        }
    }

    public async Task<Registrant?> GetRegistrantAsync(
        string webinarKey,
        string registrantKey,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}/registrants/{registrantKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var registrant = JsonSerializer.Deserialize(content, _jsonContext.Registrant);

            return registrant;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to get registrant - {ex.Message}");
            return null;
        }
    }

    public async Task<Registrant?> AddRegistrantAsync(
        string webinarKey,
        CreateRegistrantRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}/registrants";

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonContext.CreateRegistrantRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var registrant = JsonSerializer.Deserialize(responseContent, _jsonContext.Registrant);

            ClearCache();

            return registrant;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to add registrant - {ex.Message}");
            return null;
        }
    }

    public async Task<bool> RemoveRegistrantAsync(
        string webinarKey,
        string registrantKey,
        CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return false;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}/registrants/{registrantKey}";

        try
        {
            var response = await _httpClient.DeleteAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return false;
            }

            ClearCache();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to remove registrant - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return false;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Connection test failed - {ex.Message}");
            return false;
        }
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize(content, _jsonContext.ErrorResponse);

            if (error != null)
            {
                Console.WriteLine($"Error {response.StatusCode}: {error.ErrorCode} - {error.Description}");
            }
            else
            {
                Console.WriteLine($"Error {response.StatusCode}: {content}");
            }
        }
        catch
        {
            Console.WriteLine($"Error {response.StatusCode}: {response.ReasonPhrase}");
        }
    }

    private void ClearCache()
    {
        _cache.Clear();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
