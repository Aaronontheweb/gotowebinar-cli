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
    private const string RedirectUri = "http://localhost:7878/callback";
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
            
            Console.WriteLine("\n=== DEBUG: About to open browser ===");
            Console.WriteLine($"URL being opened: {authUrl}");
            Console.WriteLine("=====================================\n");
            
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

            Console.WriteLine("\n=== DEBUG: Callback Received ===");
            Console.WriteLine($"Full callback URL: {request.Url}");
            Console.WriteLine($"Query string: {request.Url?.Query}");

            var query = HttpUtility.ParseQueryString(request.Url?.Query ?? string.Empty);
            var code = query["code"];
            var returnedState = query["state"];
            var error = query["error"];
            var errorDescription = query["error_description"];

            Console.WriteLine($"Code: {(string.IsNullOrEmpty(code) ? "(none)" : code?.Substring(0, Math.Min(code.Length, 10)) + "...")}");
            Console.WriteLine($"State: {(string.IsNullOrEmpty(returnedState) ? "(none)" : returnedState)}");
            Console.WriteLine($"Error: {(string.IsNullOrEmpty(error) ? "(none)" : error)}");
            Console.WriteLine($"Error Description: {(string.IsNullOrEmpty(errorDescription) ? "(none)" : errorDescription)}");
            Console.WriteLine("=================================\n");

            if (!string.IsNullOrEmpty(error))
            {
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

        var fullUrl = $"{AuthorizationEndpoint}?{queryString}";
        
        // Log the exact URL being generated
        Console.WriteLine("\n=== DEBUG: Authorization URL Details ===");
        Console.WriteLine($"Client ID: {clientId}");
        Console.WriteLine($"Redirect URI: {RedirectUri}");
        Console.WriteLine($"State: {state}");
        Console.WriteLine($"Full URL: {fullUrl}");
        Console.WriteLine("========================================\n");
        
        return fullUrl;
    }

    private async Task<OAuthToken?> ExchangeCodeForTokenAsync(
        string code,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("\n=== DEBUG: Token Exchange Request ===");
        Console.WriteLine($"Token Endpoint: {TokenEndpoint}");
        Console.WriteLine($"Client ID: {clientId}");
        Console.WriteLine($"Redirect URI: {RedirectUri}");
        Console.WriteLine($"Code: {code?.Substring(0, Math.Min(code.Length, 10))}...");
        Console.WriteLine("=====================================\n");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code ?? string.Empty),
            new KeyValuePair<string, string>("redirect_uri", RedirectUri)
        });

        try
        {
            // Use HTTP Basic Authentication for client credentials
            var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);
            request.Content = formData;
            
            Console.WriteLine($"Using Basic Auth: {clientId}:****");
            
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            Console.WriteLine($"\n=== DEBUG: Token Response ===");
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response: {content}");
            Console.WriteLine("=============================\n");

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
