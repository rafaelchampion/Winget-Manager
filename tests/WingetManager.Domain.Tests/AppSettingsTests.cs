using FluentAssertions;
using WingetManager.Domain.Entities;
using Xunit;

namespace WingetManager.Domain.Tests;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_DefaultValues_ShouldBeConfiguredCorrectly()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.MaxConcurrentUpgrades.Should().Be(3);
        settings.AutoScanOnStartup.Should().BeTrue();
        settings.ExcludedPackageIds.Should().BeEmpty();
        settings.PinnedVersions.Should().BeEmpty();
        settings.PreferredTheme.Should().Be(AppTheme.System);
        settings.Language.Should().Be("en-US");
    }

    [Fact]
    public void AppSettings_ShouldSupportAddingExclusionsAndPins()
    {
        // Arrange
        var settings = new AppSettings
        {
            ExcludedPackageIds = ["Adobe.Acrobat"],
            PinnedVersions = [new VersionPin("Python.Python.3.11", "3.11.9", "Pinning Python 3.11")]
        };

        // Assert
        settings.ExcludedPackageIds.Should().ContainSingle().Which.Should().Be("Adobe.Acrobat");
        settings.PinnedVersions.Should().ContainSingle();
        settings.PinnedVersions[0].PackageId.Should().Be("Python.Python.3.11");
        settings.PinnedVersions[0].PinnedVersion.Should().Be("3.11.9");
    }
}
