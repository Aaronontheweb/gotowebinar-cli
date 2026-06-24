using System.CommandLine;
using System.CommandLine.Invocation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;

namespace GoToWebinarCLI.Commands;

public sealed class ConfigCommand : Command
{
    public ConfigCommand() : base("config", "Manage GoToWebinar CLI configuration and authentication")
    {
        Description = "Configure API credentials, authenticate with GoToWebinar, and manage configuration profiles. " +
                     "Get your OAuth credentials from https://developer.goto.com/oauth-clients";

        var setCommand = new Command("set", "Set GoToWebinar OAuth credentials")
        {
            Description = "Configure your OAuth Client ID and Client Secret obtained from the GoTo Developer Center. " +
                         "Both credentials are required before you can authenticate."
        };
        var clientIdOption = new Option<string>("--client-id", "OAuth Client ID from GoTo Developer Center");
        var clientSecretOption = new Option<string>("--client-secret", "OAuth Client Secret from GoTo Developer Center");
        var profileOption = new Option<string>("--profile", () => "default", "Configuration profile name (for managing multiple accounts)");

        setCommand.AddOption(clientIdOption);
        setCommand.AddOption(clientSecretOption);
        setCommand.AddOption(profileOption);

        setCommand.SetHandler(async (string? clientId, string? clientSecret, string profile) =>
        {
            await SetConfigAsync(clientId, clientSecret, profile);
        }, clientIdOption, clientSecretOption, profileOption);

        var testCommand = new Command("test", "Test your GoToWebinar API connection")
        {
            Description = "Verifies that your credentials are configured and your authentication token is valid. " +
                         "Shows token expiration time and organizer information if available."
        };
        testCommand.SetHandler(async () => await TestConnectionAsync());

        var getCommand = new Command("get", "Display current configuration settings")
        {
            Description = "Shows your current profile, API credentials (masked), authentication status, " +
                         "and other configuration settings like default format and page size."
        };
        getCommand.SetHandler(async () => await ShowConfigAsync());

        var profilesCommand = new Command("profiles", "Manage multiple GoToWebinar account profiles")
        {
            Description = "Create and switch between different configuration profiles to manage multiple GoToWebinar accounts. " +
                         "Each profile maintains its own credentials and authentication tokens."
        };

        var listProfilesCommand = new Command("list", "List all configured profiles")
        {
            Description = "Shows all available profiles, marking the current active profile with an asterisk (*) " +
                         "and indicating authentication status for each."
        };
        listProfilesCommand.SetHandler(async () => await ListProfilesAsync());

        var switchProfileCommand = new Command("switch", "Switch to a different profile")
        {
            Description = "Change the active profile to use a different set of credentials and authentication. " +
                         "The profile must already exist (created via 'config set --profile <name>')."
        };
        var profileNameArg = new Argument<string>("name", "Name of the profile to switch to");
        switchProfileCommand.AddArgument(profileNameArg);
        switchProfileCommand.SetHandler(async (string name) => await SwitchProfileAsync(name), profileNameArg);

        profilesCommand.AddCommand(listProfilesCommand);
        profilesCommand.AddCommand(switchProfileCommand);

        var authCommand = new Command("auth", "Authenticate with GoToWebinar using OAuth2 flow")
        {
            Description = "Opens your browser to authenticate with GoToWebinar. " +
                         "Requires client ID and secret to be configured first via 'config set'. " +
                         "The OAuth callback will be handled on http://localhost:7878/callback"
        };
        authCommand.SetHandler(async () => await AuthenticateAsync());

        var exportCommand = new Command("export", "Export credentials for headless/container injection")
        {
            Description = "Emits the current profile's credentials (refresh token, client ID/secret, organizer key) " +
                         "for injection into a Kubernetes Secret or env-var rotation job. Secure-by-default: writes to " +
                         "a file (0600) via --output, or prints to stdout only with the explicit --reveal flag."
        };
        var exportOutputOption = new Option<string?>(new[] { "--output", "-o" }, "Write credentials to this file (created with 0600 permissions)");
        var exportRevealOption = new Option<bool>("--reveal", "Print credentials to stdout. Use with care — stdout can be captured by logs, shell history, or agent transcripts");
        var exportFormatOption = new Option<string>("--format", () => "env", "Output format: 'env' (KEY=value lines) or 'json'");
        var exportRefreshOption = new Option<bool>("--refresh", "Force a token refresh before exporting, so rotation jobs capture GoTo's rolled refresh token");

        exportCommand.AddOption(exportOutputOption);
        exportCommand.AddOption(exportRevealOption);
        exportCommand.AddOption(exportFormatOption);
        exportCommand.AddOption(exportRefreshOption);

        exportCommand.SetHandler(
            async (string? output, bool reveal, string format, bool refresh) =>
                await ExportCredentialsAsync(output, reveal, format, refresh),
            exportOutputOption, exportRevealOption, exportFormatOption, exportRefreshOption);

        AddCommand(setCommand);
        AddCommand(authCommand);
        AddCommand(testCommand);
        AddCommand(getCommand);
        AddCommand(profilesCommand);
        AddCommand(exportCommand);
    }

    private static async Task SetConfigAsync(string? clientId, string? clientSecret, string profile)
    {
        // Validate that at least one credential is provided
        if (string.IsNullOrEmpty(clientId) && string.IsNullOrEmpty(clientSecret))
        {
            Console.WriteLine("❌ Error: You must provide at least one of --client-id or --client-secret");
            Console.WriteLine("Usage: gotowebinar config set --client-id <id> --client-secret <secret> [--profile <name>]");
            Environment.Exit(1);
            return;
        }

        var configService = new ConfigurationService();
        var config = await configService.LoadConfigAsync();

        if (!config.Profiles.TryGetValue(profile, out var configProfile))
        {
            configProfile = new ConfigProfile();
            config.Profiles[profile] = configProfile;
        }

        // When setting new credentials, warn if only one is provided and the other is missing
        bool hasExistingClientId = !string.IsNullOrEmpty(configProfile.ClientId);
        bool hasExistingClientSecret = !string.IsNullOrEmpty(configProfile.ClientSecret);

        if (!string.IsNullOrEmpty(clientId))
        {
            configProfile.ClientId = clientId;
            Console.WriteLine($"✓ Client ID set for profile '{profile}'");
        }

        if (!string.IsNullOrEmpty(clientSecret))
        {
            configProfile.ClientSecret = clientSecret;
            Console.WriteLine($"✓ Client Secret set for profile '{profile}'");
        }

        // Check if both credentials are now present
        bool nowHasClientId = !string.IsNullOrEmpty(configProfile.ClientId);
        bool nowHasClientSecret = !string.IsNullOrEmpty(configProfile.ClientSecret);

        if (!nowHasClientId || !nowHasClientSecret)
        {
            Console.WriteLine("⚠ Warning: Both client ID and client secret are required for authentication.");
            if (!nowHasClientId)
            {
                Console.WriteLine("  Missing: Client ID");
            }
            if (!nowHasClientSecret)
            {
                Console.WriteLine("  Missing: Client Secret");
            }
            Console.WriteLine("  Run 'gotowebinar config set' with the missing credential(s) to complete the configuration.");
        }

        configService.SetCurrentProfile(profile);
        await configService.SaveConfigAsync(config);
        Console.WriteLine($"✓ Configuration saved to profile '{profile}'");
    }

    private static async Task TestConnectionAsync()
    {
        var configService = new ConfigurationService();
        var config = await configService.LoadConfigAsync();
        var profile = configService.GetCurrentProfile();

        if (profile == null)
        {
            Console.WriteLine("❌ No configuration found. Run 'gotowebinar config set' first.");
            Environment.Exit(1);
            return;
        }

        if (string.IsNullOrEmpty(profile.AccessToken))
        {
            Console.WriteLine("❌ Not authenticated. Run 'gotowebinar config auth' first.");
            Environment.Exit(1);
            return;
        }

        Console.WriteLine("✓ Configuration is valid");
        Console.WriteLine($"  Profile: {config.CurrentProfile}");

        if (!string.IsNullOrEmpty(profile.OrganizerKey))
        {
            Console.WriteLine($"  Organizer Key: {profile.OrganizerKey}");
        }

        if (profile.TokenExpiry.HasValue)
        {
            var remaining = profile.TokenExpiry.Value - DateTime.UtcNow;
            if (remaining.TotalMinutes > 0)
            {
                Console.WriteLine($"  Token expires in: {remaining.Days}d {remaining.Hours}h {remaining.Minutes}m");
            }
            else
            {
                Console.WriteLine("  ⚠ Token has expired. Re-authentication needed.");
            }
        }
    }

    private static async Task ShowConfigAsync()
    {
        var configService = new ConfigurationService();
        var config = await configService.LoadConfigAsync();
        var profile = configService.GetCurrentProfile();

        Console.WriteLine("Current Configuration:");
        Console.WriteLine($"  Version: {config.Version}");
        Console.WriteLine($"  Current Profile: {config.CurrentProfile ?? "none"}");
        Console.WriteLine($"  Default Format: {config.Settings.DefaultFormat}");
        Console.WriteLine($"  Page Size: {config.Settings.PageSize}");
        Console.WriteLine($"  Auto Update: {config.Settings.AutoUpdate}");
        Console.WriteLine($"  Update Channel: {config.Settings.UpdateChannel}");

        if (profile != null)
        {
            Console.WriteLine("\nActive Profile:");
            Console.WriteLine($"  Client ID: {MaskSecret(profile.ClientId)}");
            Console.WriteLine($"  Has Token: {!string.IsNullOrEmpty(profile.AccessToken)}");

            if (profile.TokenExpiry.HasValue)
            {
                Console.WriteLine($"  Token Expiry: {profile.TokenExpiry.Value:yyyy-MM-dd HH:mm:ss} UTC");
            }

            if (!string.IsNullOrEmpty(profile.OrganizerKey))
            {
                Console.WriteLine($"  Organizer Key: {profile.OrganizerKey}");
            }
        }
    }

    private static async Task ListProfilesAsync()
    {
        var configService = new ConfigurationService();
        var config = await configService.LoadConfigAsync();

        if (config.Profiles.Count == 0)
        {
            Console.WriteLine("No profiles configured.");
            return;
        }

        Console.WriteLine("Configuration Profiles:");
        foreach (var kvp in config.Profiles)
        {
            var marker = kvp.Key == config.CurrentProfile ? " *" : "  ";
            var hasAuth = !string.IsNullOrEmpty(kvp.Value.AccessToken) ? "✓" : "✗";
            Console.WriteLine($"{marker} {kvp.Key} (authenticated: {hasAuth})");
        }
    }

    private static async Task SwitchProfileAsync(string name)
    {
        var configService = new ConfigurationService();
        var config = await configService.LoadConfigAsync();

        if (!config.Profiles.ContainsKey(name))
        {
            Console.WriteLine($"❌ Profile '{name}' does not exist.");
            Environment.Exit(1);
            return;
        }

        configService.SetCurrentProfile(name);
        await configService.SaveConfigAsync(config);
        Console.WriteLine($"✓ Switched to profile '{name}'");
    }

    private static async Task AuthenticateAsync()
    {
        var configService = new ConfigurationService();
        var httpClient = new HttpClient();
        var authService = new AuthenticationService(httpClient, configService);

        var success = await authService.AuthenticateInteractiveAsync();
        Environment.Exit(success ? 0 : 1);
    }

    private static async Task ExportCredentialsAsync(string? output, bool reveal, string format, bool refresh)
    {
        format = (format ?? "env").ToLowerInvariant();
        if (format != "env" && format != "json")
        {
            Console.Error.WriteLine($"❌ Error: Unknown format '{format}'. Use 'env' or 'json'.");
            Environment.Exit(1);
            return;
        }

        // Secure-by-default: refuse to emit secrets unless the destination is explicit.
        if (string.IsNullOrEmpty(output) && !reveal)
        {
            Console.Error.WriteLine("❌ Error: Refusing to emit credentials without an explicit destination.");
            Console.Error.WriteLine("  Use --output <file> to write a 0600 file, or --reveal to print to stdout.");
            Environment.Exit(1);
            return;
        }

        var configService = new ConfigurationService();
        await configService.LoadConfigAsync();
        var profile = configService.GetCurrentProfile();

        if (profile == null || (string.IsNullOrEmpty(profile.RefreshToken) && string.IsNullOrEmpty(profile.AccessToken)))
        {
            Console.Error.WriteLine("❌ No credentials to export. Run 'gotowebinar config auth' first.");
            Environment.Exit(1);
            return;
        }

        if (refresh)
        {
            using var httpClient = new HttpClient();
            var authService = new AuthenticationService(httpClient, configService);
            if (!await authService.RefreshTokenAsync())
            {
                Console.Error.WriteLine("❌ Error: Token refresh failed. Exported credentials would be stale.");
                Environment.Exit(1);
                return;
            }

            profile = configService.GetCurrentProfile();
        }

        // Map each credential to the env var the CLI reads it back from (see ConfigurationService).
        var credentials = new (string EnvKey, string JsonKey, string? Value)[]
        {
            ("GOTOWEBINAR_REFRESH_TOKEN", "refreshToken", profile!.RefreshToken),
            ("GOTOWEBINAR_CLIENT_ID", "clientId", profile.ClientId),
            ("GOTOWEBINAR_CLIENT_SECRET", "clientSecret", profile.ClientSecret),
            ("GOTOWEBINAR_ORGANIZER_KEY", "organizerKey", profile.OrganizerKey),
            ("GOTOWEBINAR_ACCESS_TOKEN", "accessToken", profile.AccessToken),
        };

        var content = format == "json"
            ? SerializeAsJson(credentials)
            : SerializeAsEnv(credentials);

        if (!string.IsNullOrEmpty(output))
        {
            await File.WriteAllTextAsync(output, content);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    File.SetUnixFileMode(output, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
                catch
                {
                    // Best-effort: filesystem may not support Unix permissions.
                }
            }

            Console.WriteLine($"✓ Credentials written to {output}");
            Console.WriteLine("  This file contains secrets — restrict access and delete it once injected.");
        }
        else
        {
            Console.Error.WriteLine("⚠ Emitting secrets to stdout — ensure this output is not captured by logs or history.");
            Console.Out.Write(content);
            if (!content.EndsWith('\n'))
                Console.Out.WriteLine();
        }
    }

    private static string SerializeAsEnv((string EnvKey, string JsonKey, string? Value)[] credentials)
    {
        var sb = new StringBuilder();
        foreach (var (envKey, _, value) in credentials)
        {
            if (!string.IsNullOrEmpty(value))
                sb.Append(envKey).Append('=').Append(value).Append('\n');
        }
        return sb.ToString();
    }

    private static string SerializeAsJson((string EnvKey, string JsonKey, string? Value)[] credentials)
    {
        var map = new Dictionary<string, string>();
        foreach (var (_, jsonKey, value) in credentials)
        {
            if (!string.IsNullOrEmpty(value))
                map[jsonKey] = value;
        }

        var context = new GoToWebinarJsonContext(new JsonSerializerOptions { WriteIndented = true });
        return JsonSerializer.Serialize(map, context.DictionaryStringString);
    }

    private static string? MaskSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
            return "not set";

        if (secret.Length <= 8)
            return "****";

        return $"{secret[..4]}...{secret[^4..]}";
    }
}
