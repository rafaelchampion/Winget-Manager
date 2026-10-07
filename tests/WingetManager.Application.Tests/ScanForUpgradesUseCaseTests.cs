using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WingetManager.Application.UseCases;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;
using Xunit;

namespace WingetManager.Application.Tests;

public class ScanForUpgradesUseCaseTests
{
    private readonly WingetManager.Application.Services.IPackageCacheService _cacheService = Substitute.For<WingetManager.Application.Services.IPackageCacheService>();
    private readonly ISettingsRepository _settingsRepo = Substitute.For<ISettingsRepository>();

    [Fact]
    public async Task ExecuteAsync_ShouldReturnUpgrades_ExcludingPackagesInExclusionList()
    {
        // Arrange
        var packages = new List<Package>
        {
            new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0", AvailableVersion = "2.45.0" },
            new() { Id = "Adobe.Acrobat.Reader", Name = "Adobe Acrobat Reader", InstalledVersion = "24.0", AvailableVersion = "24.1" }
        };

        var settings = new AppSettings
        {
            ExcludedPackageIds = ["Adobe.Acrobat.Reader"]
        };

        _cacheService.GetAvailableUpgradesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(packages);
        _settingsRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var useCase = new ScanForUpgradesUseCase(_cacheService, _settingsRepo, NullLogger<ScanForUpgradesUseCase>.Instance);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("Git.Git");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFilterOut_PinnedVersions()
    {
        // Arrange
        var packages = new List<Package>
        {
            new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0", AvailableVersion = "2.45.0" },
            new() { Id = "Python.Python.3.11", Name = "Python 3.11", InstalledVersion = "3.11.8", AvailableVersion = "3.11.9" }
        };

        var settings = new AppSettings
        {
            PinnedVersions = [new VersionPin("Python.Python.3.11", "3.11.8", "Stay on 3.11.8")]
        };

        _cacheService.GetAvailableUpgradesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(packages);
        _settingsRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);

        var useCase = new ScanForUpgradesUseCase(_cacheService, _settingsRepo, NullLogger<ScanForUpgradesUseCase>.Instance);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("Git.Git");
    }
}
