using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace GoToWebinarCLI.Services;

public sealed class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly string _currentVersion;
    private const string VersionManifestUrl = "https://raw.githubusercontent.com/stannardlabs/gotowebinar-cli/main/version.json";

    public UpdateService(HttpClient httpClient, string currentVersion)
    {
        _httpClient = httpClient;
        _currentVersion = currentVersion;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(VersionManifestUrl);
            var manifest = JsonSerializer.Deserialize(response, GoToWebinarJsonContext.Default.VersionManifest);

            if (manifest == null || string.IsNullOrEmpty(manifest.Version))
                return null;

            if (IsNewerVersion(manifest.Version, _currentVersion))
            {
                var platform = GetCurrentPlatform();
                var downloadUrl = platform switch
                {
                    "linux-x64" => manifest.Downloads?.LinuxX64,
                    "win-x64" => manifest.Downloads?.WinX64,
                    "osx-x64" => manifest.Downloads?.OsxX64,
                    _ => null
                };

                if (downloadUrl != null)
                {
                    return new UpdateInfo
                    {
                        Version = manifest.Version,
                        DownloadUrl = downloadUrl,
                        ReleaseDate = manifest.ReleaseDate
                    };
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
            var newParts = newVersion.Split('.').Select(int.Parse).ToArray();
            var currentParts = currentVersion.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(newParts.Length, currentParts.Length); i++)
            {
                var newPart = i < newParts.Length ? newParts[i] : 0;
                var currentPart = i < currentParts.Length ? currentParts[i] : 0;

                if (newPart > currentPart) return true;
                if (newPart < currentPart) return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
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

public sealed class VersionManifest
{
    public string Version { get; set; } = "";
    public DateTime? ReleaseDate { get; set; }
    public DownloadUrls? Downloads { get; set; }
}

public sealed class DownloadUrls
{
    public string? LinuxX64 { get; set; }
    public string? WinX64 { get; set; }
    public string? OsxX64 { get; set; }
}
