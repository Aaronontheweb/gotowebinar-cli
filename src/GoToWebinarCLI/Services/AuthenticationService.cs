using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GoToWebinarCLI.Models;

namespace GoToWebinarCLI.Services;

public class AuthenticationService
{
    private const string AuthorizationEndpoint = "https://api.getgo.com/oauth/v2/authorize";
    private const string TokenEndpoint = "https://api.getgo.com/oauth/v2/token";
    private const string RedirectUri = "http://localhost:8080/callback";
    private readonly HttpClient _httpClient;
    private readonly ConfigurationService _configService;
    private readonly GoToWebinarJsonContext _jsonContext;

    public AuthenticationService(HttpClient httpClient, ConfigurationService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
        _jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    public async Task<bool> AuthenticateInteractiveAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        if (string.IsNullOrEmpty(profile.ClientId) || string.IsNullOrEmpty(profile.ClientSecret))
        {
            Console.WriteLine("Error: Client ID and Client Secret must be configured first.");
            Console.WriteLine("Use: gotowebinar config set --client-id <id> --client-secret <secret>");
            return false;
        }

        var state = Guid.NewGuid().ToString("N");
        var authorizationCode = await GetAuthorizationCodeAsync(profile.ClientId, state, cancellationToken);

        if (string.IsNullOrEmpty(authorizationCode))
        {
            Console.WriteLine("Error: Failed to obtain authorization code.");
            return false;
        }

        var token = await ExchangeCodeForTokenAsync(
            authorizationCode,
            profile.ClientId,
            profile.ClientSecret,
            cancellationToken);

        if (token == null)
        {
            Console.WriteLine("Error: Failed to exchange authorization code for token.");
            return false;
        }

        profile.AccessToken = token.AccessToken;
        profile.RefreshToken = token.RefreshToken;
        profile.TokenExpiry = token.TokenExpiry;
        profile.OrganizerKey = token.OrganizerKey;
        profile.AccountKey = token.AccountKey;

        await _configService.SaveConfigAsync(config);

        Console.WriteLine($"✓ Successfully authenticated as {token.Email}");
        Console.WriteLine($"  Organizer Key: {token.OrganizerKey}");
        Console.WriteLine($"  Account Type: {token.AccountType}");

        return true;
    }

    private async Task<string?> GetAuthorizationCodeAsync(string clientId, string state, CancellationToken cancellationToken)
    {
        var authUrl = BuildAuthorizationUrl(clientId, state);
        var listener = new HttpListener();
        listener.Prefixes.Add(RedirectUri + "/");

        try
        {
            listener.Start();
            Console.WriteLine("Opening browser for authentication...");
            OpenBrowser(authUrl);

            Console.WriteLine($"Waiting for callback on {RedirectUri}");
            Console.WriteLine("If the browser doesn't open automatically, please visit:");
            Console.WriteLine(authUrl);

            var contextTask = listener.GetContextAsync();
            var completedTask = await Task.WhenAny(
                contextTask,
                Task.Delay(TimeSpan.FromMinutes(5), cancellationToken));

            if (completedTask != contextTask)
            {
                Console.WriteLine("Error: Authentication timeout.");
                return null;
            }

            var context = await contextTask;
            var request = context.Request;
            var response = context.Response;

            var query = HttpUtility.ParseQueryString(request.Url?.Query ?? string.Empty);
            var code = query["code"];
            var returnedState = query["state"];
            var error = query["error"];

            if (!string.IsNullOrEmpty(error))
            {
                var errorDescription = query["error_description"];
                await SendResponseAsync(response, $"<html><body><h1>Authentication Failed</h1><p>{errorDescription}</p></body></html>");
                Console.WriteLine($"Error: Authentication failed - {errorDescription}");
                return null;
            }

            if (returnedState != state)
            {
                await SendResponseAsync(response, "<html><body><h1>Authentication Failed</h1><p>Invalid state parameter.</p></body></html>");
                Console.WriteLine("Error: Invalid state parameter in callback.");
                return null;
            }

            await SendResponseAsync(response, "<html><body><h1>Authentication Successful</h1><p>You can close this window and return to the CLI.</p></body></html>");
            return code;
        }
        finally
        {
            listener.Stop();
        }
    }

    private string BuildAuthorizationUrl(string clientId, string state)
    {
        var parameters = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = RedirectUri,
            ["state"] = state
        };

        var queryString = string.Join("&",
            parameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{AuthorizationEndpoint}?{queryString}";
    }

    private async Task<OAuthToken?> ExchangeCodeForTokenAsync(
        string code,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", RedirectUri),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret)
        });

        try
        {
            var response = await _httpClient.PostAsync(TokenEndpoint, formData, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize(content, _jsonContext.OAuthErrorResponse);
                Console.WriteLine($"Error: Token exchange failed - {error?.ErrorDescription ?? content}");
                return null;
            }

            var token = JsonSerializer.Deserialize(content, _jsonContext.OAuthToken);
            token?.UpdateExpiry();
            return token;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to exchange code for token - {ex.Message}");
            return null;
        }
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        if (string.IsNullOrEmpty(profile.RefreshToken))
        {
            Console.WriteLine("Error: No refresh token available. Please authenticate first.");
            return false;
        }

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", profile.RefreshToken ?? string.Empty),
            new KeyValuePair<string, string>("client_id", profile.ClientId ?? string.Empty),
            new KeyValuePair<string, string>("client_secret", profile.ClientSecret ?? string.Empty)
        });

        try
        {
            var response = await _httpClient.PostAsync(TokenEndpoint, formData, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = JsonSerializer.Deserialize(content, _jsonContext.OAuthErrorResponse);
                Console.WriteLine($"Error: Token refresh failed - {error?.ErrorDescription ?? content}");
                return false;
            }

            var token = JsonSerializer.Deserialize(content, _jsonContext.OAuthToken);
            if (token == null)
            {
                Console.WriteLine("Error: Failed to parse token response.");
                return false;
            }

            token.UpdateExpiry();
            profile.AccessToken = token.AccessToken;
            profile.RefreshToken = token.RefreshToken;
            profile.TokenExpiry = token.TokenExpiry;
            profile.OrganizerKey = token.OrganizerKey;
            profile.AccountKey = token.AccountKey;

            await _configService.SaveConfigAsync(config);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: Failed to refresh token - {ex.Message}");
            return false;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var config = await _configService.GetConfigAsync();
        var profile = config.GetCurrentProfile();

        if (string.IsNullOrEmpty(profile.AccessToken))
            return false;

        if (profile.TokenExpiry <= DateTime.UtcNow)
        {
            return await RefreshTokenAsync();
        }

        return true;
    }

    private static async Task SendResponseAsync(HttpListenerResponse response, string html)
    {
        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html; charset=utf-8";
        await response.OutputStream.WriteAsync(buffer);
        response.OutputStream.Close();
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch
        {
        }
    }
}
