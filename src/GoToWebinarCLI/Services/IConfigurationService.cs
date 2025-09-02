using GoToWebinarCLI.Models;

namespace GoToWebinarCLI.Services;

public interface IConfigurationService
{
    Task<ConfigFile> LoadConfigAsync();
    Task<ConfigFile> GetConfigAsync();
    Task SaveConfigAsync(ConfigFile config);
    ConfigProfile? GetCurrentProfile();
    void SetCurrentProfile(string profileName);
    string GetConfigPath();
}

