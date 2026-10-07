using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WingetManager.Domain.Entities;
using WingetManager.Infrastructure.Persistence;
using Xunit;

namespace WingetManager.Infrastructure.Tests;

public class JsonSettingsRepositoryTests
{
    [Fact]
    public async Task SaveAndLoad_ShouldPersistSettingsAccurately()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"winget_settings_test_{Guid.NewGuid():N}.json");
        var repo = new JsonSettingsRepository(NullLogger<JsonSettingsRepository>.Instance, tempFile);

        var originalSettings = new AppSettings
        {
            MaxConcurrentUpgrades = 4,
            AutoScanOnStartup = false,
            ExcludedPackageIds = ["Adobe.Reader", "Microsoft.Edge"],
            PinnedVersions = [new VersionPin("Git.Git", "2.44.0", "Legacy repo requirement")],
            PreferredTheme = AppTheme.Dark,
            Language = "pt-BR"
        };

        try
        {
            // Act
            await repo.SaveAsync(originalSettings);
            var loadedSettings = await repo.LoadAsync();

            // Assert
            loadedSettings.MaxConcurrentUpgrades.Should().Be(4);
            loadedSettings.AutoScanOnStartup.Should().BeFalse();
            loadedSettings.ExcludedPackageIds.Should().BeEquivalentTo(["Adobe.Reader", "Microsoft.Edge"]);
            loadedSettings.PinnedVersions.Should().HaveCount(1);
            loadedSettings.PinnedVersions[0].PackageId.Should().Be("Git.Git");
            loadedSettings.PinnedVersions[0].PinnedVersion.Should().Be("2.44.0");
            loadedSettings.PreferredTheme.Should().Be(AppTheme.Dark);
            loadedSettings.Language.Should().Be("pt-BR");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
