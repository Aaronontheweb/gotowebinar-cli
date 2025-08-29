using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoToWebinarCLI.Models;

namespace GoToWebinarCLI.Services;

public sealed class ConfigurationService
{
    private readonly string _configPath;
    private readonly GoToWebinarJsonContext _jsonContext;
    private ConfigFile? _config;
    private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("GoToWebinar-CLI-2024");

    public ConfigurationService()
    {
        _jsonContext = new GoToWebinarJsonContext(new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".gotowebinar");

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetUnixFilePermissions(configDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        _configPath = Path.Combine(configDir, "config.json");
    }

    public async Task<ConfigFile> LoadConfigAsync()
    {
        if (_config != null)
            return _config;

        if (!File.Exists(_configPath))
        {
            _config = new ConfigFile();
            await SaveConfigAsync(_config);
            return _config;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configPath);
            var config = JsonSerializer.Deserialize(json, _jsonContext.ConfigFile);

            if (config == null)
            {
                _config = new ConfigFile();
                await SaveConfigAsync(_config);
                return _config;
            }

            DecryptSecrets(config);
            _config = config;
            return _config;
        }
        catch
        {
            _config = new ConfigFile();
            await SaveConfigAsync(_config);
            return _config;
        }
    }

    public async Task SaveConfigAsync(ConfigFile config)
    {
        var configToSave = new ConfigFile
        {
            Version = config.Version,
            CurrentProfile = config.CurrentProfile,
            Settings = config.Settings,
            Profiles = new Dictionary<string, ConfigProfile>()
        };

        foreach (var kvp in config.Profiles)
        {
            configToSave.Profiles[kvp.Key] = EncryptProfile(kvp.Value);
        }

        var json = JsonSerializer.Serialize(configToSave, _jsonContext.ConfigFile);
        await File.WriteAllTextAsync(_configPath, json);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            SetUnixFilePermissions(_configPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        _config = config;
    }

    public ConfigProfile? GetCurrentProfile()
    {
        if (_config == null)
            return null;

        if (string.IsNullOrEmpty(_config.CurrentProfile))
            return null;

        return _config.Profiles.TryGetValue(_config.CurrentProfile, out var profile) ? profile : null;
    }

    public void SetCurrentProfile(string profileName)
    {
        if (_config == null)
            return;

        if (_config.Profiles.ContainsKey(profileName))
        {
            _config.CurrentProfile = profileName;
        }
    }

    public void AddOrUpdateProfile(string profileName, ConfigProfile profile)
    {
        _config ??= new ConfigFile();
        _config.Profiles[profileName] = profile;

        if (string.IsNullOrEmpty(_config.CurrentProfile))
        {
            _config.CurrentProfile = profileName;
        }
    }

    public async Task<ConfigFile> GetConfigAsync()
    {
        return await LoadConfigAsync();
    }

    private ConfigProfile EncryptProfile(ConfigProfile profile)
    {
        return new ConfigProfile
        {
            ClientId = Encrypt(profile.ClientId),
            ClientSecret = Encrypt(profile.ClientSecret),
            AccessToken = Encrypt(profile.AccessToken),
            RefreshToken = Encrypt(profile.RefreshToken),
            TokenExpiry = profile.TokenExpiry,
            OrganizerKey = profile.OrganizerKey,
            AccountKey = profile.AccountKey
        };
    }

    private void DecryptSecrets(ConfigFile config)
    {
        foreach (var kvp in config.Profiles)
        {
            var profile = kvp.Value;
            profile.ClientId = Decrypt(profile.ClientId);
            profile.ClientSecret = Decrypt(profile.ClientSecret);
            profile.AccessToken = Decrypt(profile.AccessToken);
            profile.RefreshToken = Decrypt(profile.RefreshToken);
        }
    }

    private static string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var cipherBytes = ProtectedData.Protect(plainBytes, _entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(cipherBytes);
            }
            else
            {
                var key = GenerateKeyFromEntropy();
                using var aes = Aes.Create();
                aes.Key = key;
                aes.GenerateIV();

                using var ms = new MemoryStream();
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
        catch
        {
            return plainText;
        }
    }

    private static string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var cipherBytes = Convert.FromBase64String(cipherText);
                var plainBytes = ProtectedData.Unprotect(cipherBytes, _entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            else
            {
                var key = GenerateKeyFromEntropy();
                var cipherBytes = Convert.FromBase64String(cipherText);

                using var ms = new MemoryStream(cipherBytes);
                var iv = new byte[16];
                ms.Read(iv, 0, iv.Length);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
        }
        catch
        {
            return cipherText;
        }
    }

    private static byte[] GenerateKeyFromEntropy()
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(_entropy);
    }

    [UnsupportedOSPlatform("windows")]
    private static void SetUnixFilePermissions(string path, UnixFileMode mode)
    {
        try
        {
            File.SetUnixFileMode(path, mode);
        }
        catch
        {
        }
    }
}
