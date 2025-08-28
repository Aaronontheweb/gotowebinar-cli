using System.CommandLine;
using System.CommandLine.Invocation;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;

namespace GoToWebinarCLI.Commands;

public sealed class ConfigCommand : Command
{
    public ConfigCommand() : base("config", "Manage GoToWebinar CLI configuration")
    {
        var setCommand = new Command("set", "Set configuration values");
        var clientIdOption = new Option<string>("--client-id", "OAuth Client ID");
        var clientSecretOption = new Option<string>("--client-secret", "OAuth Client Secret");
        var profileOption = new Option<string>("--profile", () => "default", "Configuration profile name");

        setCommand.AddOption(clientIdOption);
        setCommand.AddOption(clientSecretOption);
        setCommand.AddOption(profileOption);

        setCommand.SetHandler(async (string? clientId, string? clientSecret, string profile) =>
        {
            await SetConfigAsync(clientId, clientSecret, profile);
        }, clientIdOption, clientSecretOption, profileOption);

        var testCommand = new Command("test", "Test API connection");
        testCommand.SetHandler(async () => await TestConnectionAsync());

        var getCommand = new Command("get", "Show current configuration");
        getCommand.SetHandler(async () => await ShowConfigAsync());

        var profilesCommand = new Command("profiles", "Manage configuration profiles");

        var listProfilesCommand = new Command("list", "List all profiles");
        listProfilesCommand.SetHandler(async () => await ListProfilesAsync());

        var switchProfileCommand = new Command("switch", "Switch to a different profile");
        var profileNameArg = new Argument<string>("name", "Profile name to switch to");
        switchProfileCommand.AddArgument(profileNameArg);
        switchProfileCommand.SetHandler(async (string name) => await SwitchProfileAsync(name), profileNameArg);

        profilesCommand.AddCommand(listProfilesCommand);
        profilesCommand.AddCommand(switchProfileCommand);

        AddCommand(setCommand);
        AddCommand(testCommand);
        AddCommand(getCommand);
        AddCommand(profilesCommand);
    }

    private static async Task SetConfigAsync(string? clientId, string? clientSecret, string profile)
    {
        var configService = new ConfigurationService();
        var config = await configService.LoadConfigAsync();

        if (!config.Profiles.TryGetValue(profile, out var configProfile))
        {
            configProfile = new ConfigProfile();
            config.Profiles[profile] = configProfile;
        }

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

    private static string? MaskSecret(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
            return "not set";

        if (secret.Length <= 8)
            return "****";

        return $"{secret[..4]}...{secret[^4..]}";
    }
}