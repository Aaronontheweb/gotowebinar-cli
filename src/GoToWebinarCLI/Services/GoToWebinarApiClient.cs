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

        // Always use provided dates when they are specified
        var from = fromTime ?? DateTime.UtcNow;
        var to = toTime ?? DateTime.UtcNow.AddMonths(12);

        var cacheKey = $"webinars_{upcoming}_{from:yyyyMMdd}_{to:yyyyMMdd}";

        if (_cache.TryGetValue(cacheKey, out var cached) && cached.Expiry > DateTime.UtcNow)
        {
            return JsonSerializer.Deserialize(cached.Data, _jsonContext.ListWebinar);
        }

        try
        {
            var allWebinars = new List<Webinar>();
            var pageNumber = 0;
            var pageSize = 100; // Default page size
            var hasMorePages = true;

            while (hasMorePages)
            {
                var url = $"organizers/{profile.OrganizerKey}/webinars" +
                          $"?fromTime={from:yyyy-MM-ddTHH:mm:ssZ}" +
                          $"&toTime={to:yyyy-MM-ddTHH:mm:ssZ}" +
                          $"&page={pageNumber}" +
                          $"&size={pageSize}";

                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponseAsync(response);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                // Check if response contains _embedded structure
                if (content.Contains("\"_embedded\""))
                {
                    var pagedResponse = JsonSerializer.Deserialize(content, _jsonContext.PagedResponseWebinar);
                    if (pagedResponse?.Embedded?.Webinars != null)
                    {
                        allWebinars.AddRange(pagedResponse.Embedded.Webinars);
                    }

                    // Check if there are more pages
                    if (pagedResponse?.Page != null)
                    {
                        hasMorePages = (pageNumber + 1) < pagedResponse.Page.TotalPages;
                        pageNumber++;
                    }
                    else
                    {
                        hasMorePages = false;
                    }
                }
                else
                {
                    // Empty response or no more pages
                    hasMorePages = false;
                }
            }

            // Cache the combined results
            var cacheData = JsonSerializer.Serialize(allWebinars, _jsonContext.ListWebinar);
            _cache[cacheKey] = (DateTime.UtcNow.Add(_cacheExpiry), cacheData);

            return allWebinars;
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

    public async Task<Webinar?> UpdateWebinarAsync(string webinarKey, UpdateWebinarRequest request, CancellationToken cancellationToken = default)
    {
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}";

        try
        {
            var json = JsonSerializer.Serialize(request, _jsonContext.UpdateWebinarRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return null;
            }

            // Clear cache since we've updated a webinar
            ClearCache();

            // Fetch and return the updated webinar
            return await GetWebinarAsync(webinarKey, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to update webinar - {ex.Message}");
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
            var basicRegistrants = JsonSerializer.Deserialize(content, _jsonContext.ListRegistrant);

            if (basicRegistrants == null || basicRegistrants.Count == 0)
                return basicRegistrants;

            // The list endpoint returns condensed records without organization, jobTitle, and
            // other profile fields. Fetch full details for each registrant in parallel;
            // the RateLimitHandler caps concurrency to 10 req/s automatically.
            var detailTasks = basicRegistrants.Select(r =>
                GetRegistrantAsync(webinarKey, r.RegistrantKey.ToString(), cancellationToken));

            var detailed = await Task.WhenAll(detailTasks);

            var result = detailed
                .Select((d, i) => d ?? basicRegistrants[i])
                .ToList();

            var cacheData = JsonSerializer.Serialize(result, _jsonContext.ListRegistrant);
            _cache[cacheKey] = (DateTime.UtcNow.Add(_cacheExpiry), cacheData);

            return result;
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

    /// <summary>
    /// DEPRECATED: This API endpoint doesn't exist in GoToWebinar v2.
    /// Registration fields must be configured through the web UI.
    /// See: https://github.com/Aaronontheweb/gotowebinar-cli/issues/45
    /// </summary>
    [Obsolete("This API endpoint doesn't exist in GoToWebinar v2. Use the web UI to configure registration fields.")]
    public async Task<RegistrationFields?> GetRegistrationFieldsAsync(
        string webinarKey,
        CancellationToken cancellationToken = default)
    {
        // This endpoint doesn't exist - always return null
        await Task.CompletedTask; // Suppress async warning
        return null;

        // Original implementation kept for reference:
        /*
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}/registrationFields";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var fields = JsonSerializer.Deserialize(content, _jsonContext.RegistrationFields);

            return fields;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to get registration fields - {ex.Message}");
            return null;
        }
        */
    }

    /// <summary>
    /// DEPRECATED: This API endpoint doesn't exist in GoToWebinar v2.
    /// Registration fields must be configured through the web UI.
    /// See: https://github.com/Aaronontheweb/gotowebinar-cli/issues/45
    /// </summary>
    [Obsolete("This API endpoint doesn't exist in GoToWebinar v2. Use the web UI to configure registration fields.")]
    public async Task<bool> UpdateRegistrationFieldsAsync(
        string webinarKey,
        RegistrationFields fields,
        CancellationToken cancellationToken = default)
    {
        // This endpoint doesn't exist - always return false
        await Task.CompletedTask; // Suppress async warning
        return false;

        // Original implementation kept for reference:
        /*
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return false;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}/registrationFields";

        try
        {
            var json = JsonSerializer.Serialize(fields, _jsonContext.RegistrationFields);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                await HandleErrorResponseAsync(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to update registration fields - {ex.Message}");
            return false;
        }
        */
    }

    /// <summary>
    /// DEPRECATED: This API endpoint doesn't exist in GoToWebinar v2.
    /// Email settings must be configured through the web UI.
    /// See: https://github.com/Aaronontheweb/gotowebinar-cli/issues/45
    /// </summary>
    [Obsolete("This API endpoint doesn't exist in GoToWebinar v2. Use the web UI to configure email settings.")]
    public async Task<EmailSettings?> GetEmailSettingsAsync(
        string webinarKey,
        CancellationToken cancellationToken = default)
    {
        // This endpoint doesn't exist - always return null
        await Task.CompletedTask; // Suppress async warning
        return null;

        // Original implementation kept for reference:
        /*
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return null;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        // Note: This endpoint might not exist in GoToWebinar API v2
        // We may need to get this from the webinar details instead
        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}/emailSettings";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Email settings might be part of webinar details
                // For now, return null if endpoint doesn't exist
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Note: Email settings endpoint not available in current API version");
                    return null;
                }

                await HandleErrorResponseAsync(response);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var settings = JsonSerializer.Deserialize(content, _jsonContext.EmailSettings);

            return settings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to get email settings - {ex.Message}");
            return null;
        }
        */
    }

    /// <summary>
    /// DEPRECATED: This API endpoint doesn't exist in GoToWebinar v2.
    /// Email settings must be configured through the web UI.
    /// See: https://github.com/Aaronontheweb/gotowebinar-cli/issues/45
    /// </summary>
    [Obsolete("This API endpoint doesn't exist in GoToWebinar v2. Use the web UI to configure email settings.")]
    public async Task<bool> UpdateEmailSettingsAsync(
        string webinarKey,
        EmailSettings settings,
        CancellationToken cancellationToken = default)
    {
        // This endpoint doesn't exist - always return false
        await Task.CompletedTask; // Suppress async warning
        return false;

        // Original implementation kept for reference:
        /*
        if (!await EnsureAuthenticatedAsync(cancellationToken))
            return false;

        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        // Note: This endpoint might not exist in GoToWebinar API v2
        var url = $"organizers/{profile.OrganizerKey}/webinars/{webinarKey}/emailSettings";

        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonContext.EmailSettings);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Note: Email settings endpoint not available in current API version");
                    return false;
                }

                await HandleErrorResponseAsync(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to update email settings - {ex.Message}");
            return false;
        }
        */
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
