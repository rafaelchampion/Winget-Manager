using FluentAssertions;
using WingetManager.Domain.Entities;
using Xunit;

namespace WingetManager.Domain.Tests;

public class PackageTests
{
    [Fact]
    public void HasUpdate_ShouldBeTrue_WhenAvailableVersionDiffersFromInstalledVersion()
    {
        // Arrange
        var package = new Package
        {
            Id = "Git.Git",
            Name = "Git",
            InstalledVersion = "2.44.0",
            AvailableVersion = "2.45.0",
            Source = "winget"
        };

        // Act & Assert
        package.HasUpdate.Should().BeTrue();
    }

    [Fact]
    public void HasUpdate_ShouldBeFalse_WhenAvailableVersionIsNull()
    {
        // Arrange
        var package = new Package
        {
            Id = "Git.Git",
            Name = "Git",
            InstalledVersion = "2.44.0",
            AvailableVersion = null,
            Source = "winget"
        };

        // Act & Assert
        package.HasUpdate.Should().BeFalse();
    }

    [Fact]
    public void HasUpdate_ShouldBeFalse_WhenAvailableVersionMatchesInstalledVersion()
    {
        // Arrange
        var package = new Package
        {
            Id = "Git.Git",
            Name = "Git",
            InstalledVersion = "2.45.0",
            AvailableVersion = "2.45.0",
            Source = "winget"
        };

        // Act & Assert
        package.HasUpdate.Should().BeFalse();
    }
}
