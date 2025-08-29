using System.CommandLine;
using System.Reflection;
using GoToWebinarCLI.Services;

namespace GoToWebinarCLI.Commands;

public static class UpdateCommand
{
    public static Command Create()
    {
        var command = new Command("update", "Check for and install updates");

        var checkOnlyOption = new Option<bool>(
            new[] { "--check", "-c" },
            "Only check for updates without installing");

        var forceOption = new Option<bool>(
            new[] { "--force", "-f" },
            "Force update even if current version is up to date");

        command.AddOption(checkOnlyOption);
        command.AddOption(forceOption);

        command.SetHandler(async (checkOnly, force) =>
        {
            var currentVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "1.0.0";

            Console.WriteLine($"Current version: {currentVersion}");
            Console.WriteLine("Checking for updates...");

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"GoToWebinarCLI/{currentVersion}");

            var updateService = new UpdateService(httpClient, currentVersion);
            var updateInfo = await updateService.CheckForUpdateAsync();

            if (updateInfo == null)
            {
                Console.WriteLine("You are running the latest version.");
                return;
            }

            Console.WriteLine($"New version available: {updateInfo.Version}");
            if (updateInfo.ReleaseDate.HasValue)
            {
                Console.WriteLine($"Released: {updateInfo.ReleaseDate.Value:yyyy-MM-dd}");
            }

            if (checkOnly)
            {
                Console.WriteLine("Run 'gotowebinar update' to install the update.");
                return;
            }

            Console.Write("Do you want to install the update? [Y/n]: ");
            var response = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (response != "y" && response != "yes" && !string.IsNullOrEmpty(response))
            {
                Console.WriteLine("Update cancelled.");
                return;
            }

            var success = await updateService.DownloadAndInstallUpdateAsync(updateInfo);

            if (success)
            {
                Environment.Exit(0); // Exit to allow restart with new version
            }
            else
            {
                Environment.Exit(1);
            }
        }, checkOnlyOption, forceOption);

        return command;
    }
}
