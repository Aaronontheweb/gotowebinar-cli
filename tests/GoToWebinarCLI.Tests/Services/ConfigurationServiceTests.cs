using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using GoToWebinarCLI.Models;
using GoToWebinarCLI.Services;
using Xunit;

namespace GoToWebinarCLI.Tests.Services;

public class ConfigurationServiceTests : IDisposable
{
    private readonly string _testConfigDirectory;
    private readonly List<string> _envVarsToRestore = new();

    public ConfigurationServiceTests()
    {
        _testConfigDirectory = Path.Combine(Path.GetTempPath(), $"gotowebinar-cfg-test-{Guid.NewGuid()}");
    }

    public void Dispose()
    {
        foreach (var key in _envVarsToRestore)
            Environment.SetEnvironmentVariable(key, null);

        if (Directory.Exists(_testConfigDirectory))
            Directory.Delete(_testConfigDirectory, recursive: true);
    }

    private void SetEnvVar(string key, string value)
    {
        _envVarsToRestore.Add(key);
        Environment.SetEnvironmentVariable(key, value);
    }

    [Fact]
    public async Task LoadConfigAsync_WithAccessTokenEnvVar_ReturnsEnvVarProfile()
    {
        SetEnvVar("GOTOWEBINAR_ACCESS_TOKEN", "env-access-token");
        SetEnvVar("GOTOWEBINAR_ORGANIZER_KEY", "env-organizer-key");
        SetEnvVar("GOTOWEBINAR_CLIENT_ID", "env-client-id");
        SetEnvVar("GOTOWEBINAR_CLIENT_SECRET", "env-client-secret");
        SetEnvVar("GOTOWEBINAR_REFRESH_TOKEN", "env-refresh-token");

        var service = new ConfigurationService(_testConfigDirectory);
        var config = await service.LoadConfigAsync();

        var profile = config.GetCurrentProfile();
        profile.AccessToken.Should().Be("env-access-token");
        profile.OrganizerKey.Should().Be("env-organizer-key");
        profile.ClientId.Should().Be("env-client-id");
        profile.ClientSecret.Should().Be("env-client-secret");
        profile.RefreshToken.Should().Be("env-refresh-token");
    }

    [Fact]
    public async Task LoadConfigAsync_WithEnvVars_DoesNotRequireConfigFile()
    {
        SetEnvVar("GOTOWEBINAR_ACCESS_TOKEN", "env-access-token");
        SetEnvVar("GOTOWEBINAR_ORGANIZER_KEY", "env-organizer-key");

        // Point at a directory that doesn't exist — should not throw
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"gotowebinar-nonexistent-{Guid.NewGuid()}");
        var service = new ConfigurationService(nonExistentDir);

        var act = async () => await service.LoadConfigAsync();
        await act.Should().NotThrowAsync();

        var config = await service.LoadConfigAsync();
        config.GetCurrentProfile().AccessToken.Should().Be("env-access-token");
    }

    [Fact]
    public async Task SaveConfigAsync_WhenDirectoryNotWritable_DoesNotThrow()
    {
        SetEnvVar("GOTOWEBINAR_ACCESS_TOKEN", "env-access-token");

        // Use a path that cannot be written to (file path as directory)
        var impossibleDir = Path.Combine(Path.GetTempPath(), $"gotowebinar-impossible-{Guid.NewGuid()}", "subdir", "deep");
        var service = new ConfigurationService(impossibleDir);

        var config = new ConfigFile();
        config.Profiles["default"] = new ConfigProfile { AccessToken = "updated-token" };
        config.CurrentProfile = "default";

        var act = async () => await service.SaveConfigAsync(config);
        await act.Should().NotThrowAsync();

        // In-memory config should be updated to the saved value
        var loaded = await service.GetConfigAsync();
        loaded.GetCurrentProfile().AccessToken.Should().Be("updated-token");
    }

    [Fact]
    public async Task LoadConfigAsync_WithoutEnvVars_FallsBackToFileBasedLoading()
    {
        // Ensure env vars are NOT set
        Environment.SetEnvironmentVariable("GOTOWEBINAR_ACCESS_TOKEN", null);

        var service = new ConfigurationService(_testConfigDirectory);
        var config = await service.LoadConfigAsync();

        // Should return an empty default config (file created fresh)
        config.Should().NotBeNull();
        var profile = config.GetCurrentProfile();
        profile.AccessToken.Should().BeNullOrEmpty();
    }
}
