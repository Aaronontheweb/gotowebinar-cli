using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GoToWebinarCLI.Services;

public sealed class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _currentVersion;
    private const string GitHubApiUrl = "https://api.github.com/repos/stannardlabs/gotowebinar-cli/releases/latest";

    public UpdateService(HttpClient httpClient, string currentVersion)
    {
        _httpClient = httpClient;
        _currentVersion = currentVersion;
        
        // Add User-Agent header required by GitHub API
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GoToWebinar-CLI");
        }
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(GitHubApiUrl);
            var releaseJson = JsonNode.Parse(response);
            
            if (releaseJson == null)
                return null;

            var tagName = releaseJson["tag_name"]?.ToString();
            if (string.IsNullOrEmpty(tagName))
                return null;

            // Remove 'v' prefix if present
            var version = tagName.StartsWith("v") ? tagName.Substring(1) : tagName;
            
            // Parse current version to handle versions with build metadata (e.g., "1.0.0-beta1+hash")
            var currentVersionClean = _currentVersion.Split('+')[0];

            if (IsNewerVersion(version, currentVersionClean))
            {
                var platform = GetCurrentPlatform();
                var assetName = platform switch
                {
                    "linux-x64" => "gotowebinar-linux-x64.tar.gz",
                    "win-x64" => "gotowebinar-win-x64.exe.zip",
                    "osx-x64" => "gotowebinar-osx-x64.tar.gz",
                    _ => null
                };

                if (assetName == null)
                    return null;

                // Find the matching asset
                var assets = releaseJson["assets"]?.AsArray();
                if (assets != null)
                {
                    foreach (var asset in assets)
                    {
                        if (asset?["name"]?.ToString() == assetName)
                        {
                            var downloadUrl = asset["browser_download_url"]?.ToString();
                            if (!string.IsNullOrEmpty(downloadUrl))
                            {
                                var releaseDateStr = releaseJson["published_at"]?.ToString();
                                DateTime? releaseDate = null;
                                if (!string.IsNullOrEmpty(releaseDateStr))
                                {
                                    DateTime.TryParse(releaseDateStr, out var parsedDate);
                                    releaseDate = parsedDate;
                                }

                                return new UpdateInfo
                                {
                                    Version = version,
                                    DownloadUrl = downloadUrl,
                                    ReleaseDate = releaseDate
                                };
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to check for updates: {ex.Message}");
        }

        return null;
    }

    public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
    {
        try
        {
            Console.WriteLine($"Downloading version {updateInfo.Version}...");

            var tempFile = Path.GetTempFileName();
            using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl))
            {
                response.EnsureSuccessStatusCode();
                await using var fs = new FileStream(tempFile, FileMode.Create);
                await response.Content.CopyToAsync(fs);
            }

            Console.WriteLine("Download complete. Installing update...");

            var currentExecutable = Process.GetCurrentProcess().MainModule?.FileName;
            if (currentExecutable == null)
            {
                Console.Error.WriteLine("Could not determine current executable path");
                return false;
            }

            // Extract based on platform
            var platform = GetCurrentPlatform();
            string extractedFile;

            if (platform == "win-x64")
            {
                // Extract zip file on Windows
                extractedFile = ExtractZipFile(tempFile);
            }
            else
            {
                // Extract tar.gz file on Linux/macOS
                extractedFile = ExtractTarGzFile(tempFile);
            }

            // Create backup of current executable
            var backupFile = currentExecutable + ".backup";
            File.Move(currentExecutable, backupFile, true);

            try
            {
                // Move new executable to current location
                File.Move(extractedFile, currentExecutable, true);

                // Set executable permissions on Unix systems
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var chmod = Process.Start("chmod", $"+x \"{currentExecutable}\"");
                    chmod?.WaitForExit();
                }

                Console.WriteLine($"Update to version {updateInfo.Version} completed successfully!");
                Console.WriteLine("Please restart the application to use the new version.");

                // Clean up backup
                try { File.Delete(backupFile); } catch { }

                return true;
            }
            catch
            {
                // Restore backup on failure
                Console.Error.WriteLine("Update failed, restoring previous version...");
                File.Move(backupFile, currentExecutable, true);
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to install update: {ex.Message}");
            return false;
        }
    }

    private static string GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "win-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux-x64";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "osx-x64";

        return "unknown";
    }

    private static bool IsNewerVersion(string newVersion, string currentVersion)
    {
        try
        {
            // Handle semantic versioning with pre-release tags (e.g., 1.0.0-beta1)
            var newParts = ParseVersion(newVersion);
            var currentParts = ParseVersion(currentVersion);

            // Compare major.minor.patch first
            for (int i = 0; i < 3; i++)
            {
                if (newParts.Major[i] > currentParts.Major[i]) return true;
                if (newParts.Major[i] < currentParts.Major[i]) return false;
            }

            // If major.minor.patch are the same, compare pre-release
            // No pre-release is considered newer than any pre-release
            if (string.IsNullOrEmpty(newParts.PreRelease) && !string.IsNullOrEmpty(currentParts.PreRelease))
                return true;
            if (!string.IsNullOrEmpty(newParts.PreRelease) && string.IsNullOrEmpty(currentParts.PreRelease))
                return false;
            
            // If both have pre-release, compare them
            if (!string.IsNullOrEmpty(newParts.PreRelease) && !string.IsNullOrEmpty(currentParts.PreRelease))
            {
                return string.Compare(newParts.PreRelease, currentParts.PreRelease, StringComparison.OrdinalIgnoreCase) > 0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static (int[] Major, string PreRelease) ParseVersion(string version)
    {
        var dashIndex = version.IndexOf('-');
        var majorPart = dashIndex >= 0 ? version.Substring(0, dashIndex) : version;
        var preRelease = dashIndex >= 0 ? version.Substring(dashIndex + 1) : "";

        var parts = majorPart.Split('.').Select(p => int.TryParse(p, out var num) ? num : 0).ToArray();
        
        // Ensure we have at least 3 parts (major.minor.patch)
        var major = new int[3];
        for (int i = 0; i < Math.Min(parts.Length, 3); i++)
        {
            major[i] = parts[i];
        }

        return (major, preRelease);
    }

    private static string ExtractZipFile(string zipPath)
    {
        var extractPath = Path.GetDirectoryName(zipPath)!;
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath, true);
        return Path.Combine(extractPath, "gotowebinar.exe");
    }

    private static string ExtractTarGzFile(string tarGzPath)
    {
        var extractPath = Path.GetDirectoryName(tarGzPath)!;

        // Use tar command to extract
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"-xzf \"{tarGzPath}\" -C \"{extractPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        process?.WaitForExit();

        return Path.Combine(extractPath, "gotowebinar");
    }
}

public sealed class UpdateInfo
{
    public string Version { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public DateTime? ReleaseDate { get; set; }
}
