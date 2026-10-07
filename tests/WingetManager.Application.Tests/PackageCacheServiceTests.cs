using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WingetManager.Application.Services;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;
using Xunit;

namespace WingetManager.Application.Tests;

public class PackageCacheServiceTests
{
    private readonly IPackageRepository _packageRepo = Substitute.For<IPackageRepository>();
    private readonly IPackageCacheStore _cacheStore = Substitute.For<IPackageCacheStore>();
    private readonly ISettingsRepository _settingsRepo = Substitute.For<ISettingsRepository>();

    public PackageCacheServiceTests()
    {
        _settingsRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(new AppSettings { CacheTtlMinutes = 15 });
    }

    [Fact]
    public async Task GetInstalledPackagesAsync_WhenCacheFresh_ShouldReturnCachedData_WithoutCallingRepo()
    {
        // Arrange
        var cached = new PackageCacheData
        {
            InstalledPackages =
            [
                new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0" }
            ],
            InstalledPackagesScannedAt = DateTimeOffset.UtcNow.AddMinutes(-5) // 5 mins ago (< 15 mins)
        };
        _cacheStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(cached);

        var service = new PackageCacheService(_packageRepo, _cacheStore, _settingsRepo, NullLogger<PackageCacheService>.Instance);

        // Act
        var result = await service.GetInstalledPackagesAsync(forceRefresh: false);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("Git.Git");
        await _packageRepo.DidNotReceive().GetInstalledPackagesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInstalledPackagesAsync_WhenForceRefresh_ShouldCallRepo_AndSaveToStore()
    {
        // Arrange
        var cached = new PackageCacheData
        {
            InstalledPackages =
            [
                new() { Id = "Old.Pkg", Name = "Old", InstalledVersion = "1.0" }
            ],
            InstalledPackagesScannedAt = DateTimeOffset.UtcNow.AddMinutes(-2)
        };
        _cacheStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(cached);

        var freshList = new List<Package>
        {
            new() { Id = "New.Pkg", Name = "New", InstalledVersion = "2.0" }
        };
        _packageRepo.GetInstalledPackagesAsync(Arg.Any<CancellationToken>()).Returns(freshList);

        var service = new PackageCacheService(_packageRepo, _cacheStore, _settingsRepo, NullLogger<PackageCacheService>.Instance);

        // Act
        var result = await service.GetInstalledPackagesAsync(forceRefresh: true);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("New.Pkg");
        await _packageRepo.Received(1).GetInstalledPackagesAsync(Arg.Any<CancellationToken>());
        await _cacheStore.Received(1).SaveAsync(Arg.Is<PackageCacheData>(d => d.InstalledPackages.Count == 1 && d.InstalledPackages[0].Id == "New.Pkg"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetInstalledPackagesAsync_WhenCacheExpired_ShouldCallRepo_AndSaveToStore()
    {
        // Arrange
        var cached = new PackageCacheData
        {
            InstalledPackages =
            [
                new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0" }
            ],
            InstalledPackagesScannedAt = DateTimeOffset.UtcNow.AddMinutes(-30) // 30 mins ago (> 15 mins TTL)
        };
        _cacheStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(cached);

        var freshList = new List<Package>
        {
            new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.45.0" }
        };
        _packageRepo.GetInstalledPackagesAsync(Arg.Any<CancellationToken>()).Returns(freshList);

        var service = new PackageCacheService(_packageRepo, _cacheStore, _settingsRepo, NullLogger<PackageCacheService>.Instance);

        // Act
        var result = await service.GetInstalledPackagesAsync(forceRefresh: false);

        // Assert
        result.Should().HaveCount(1);
        result[0].InstalledVersion.Should().Be("2.45.0");
        await _packageRepo.Received(1).GetInstalledPackagesAsync(Arg.Any<CancellationToken>());
        await _cacheStore.Received(1).SaveAsync(Arg.Any<PackageCacheData>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkPackageUpgradedAsync_ShouldRemoveFromUpgrades_AndUpdateInstalledVersion()
    {
        // Arrange
        var cached = new PackageCacheData
        {
            InstalledPackages =
            [
                new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0", AvailableVersion = "2.45.0" }
            ],
            UpgradablePackages =
            [
                new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0", AvailableVersion = "2.45.0" }
            ],
            InstalledPackagesScannedAt = DateTimeOffset.UtcNow,
            UpgradesScannedAt = DateTimeOffset.UtcNow
        };
        _cacheStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(cached);

        var service = new PackageCacheService(_packageRepo, _cacheStore, _settingsRepo, NullLogger<PackageCacheService>.Instance);

        // Act
        await service.MarkPackageUpgradedAsync("Git.Git", "2.45.0");

        // Assert
        var upgrades = await service.GetAvailableUpgradesAsync(forceRefresh: false);
        upgrades.Should().BeEmpty();

        var installed = await service.GetInstalledPackagesAsync(forceRefresh: false);
        installed.Should().HaveCount(1);
        installed[0].InstalledVersion.Should().Be("2.45.0");
        installed[0].AvailableVersion.Should().BeNull();

        await _cacheStore.Received(1).SaveAsync(Arg.Is<PackageCacheData>(d => d.UpgradablePackages.Count == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateCacheAsync_ShouldClearCache_AndSaveEmpty()
    {
        // Arrange
        var cached = new PackageCacheData
        {
            InstalledPackages = [new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0" }],
            InstalledPackagesScannedAt = DateTimeOffset.UtcNow
        };
        _cacheStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(cached);

        var service = new PackageCacheService(_packageRepo, _cacheStore, _settingsRepo, NullLogger<PackageCacheService>.Instance);

        // Act
        await service.InvalidateCacheAsync();

        // Assert
        await _cacheStore.Received(1).SaveAsync(Arg.Is<PackageCacheData>(d => d.InstalledPackages.Count == 0 && d.InstalledPackagesScannedAt == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCachedSnapshotAsync_ShouldReturnCachedData_ImmediatelyWithoutCallingRepo()
    {
        // Arrange
        var cached = new PackageCacheData
        {
            InstalledPackages = [new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0" }],
            UpgradablePackages = [new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0", AvailableVersion = "2.45.0" }],
            InstalledPackagesScannedAt = DateTimeOffset.UtcNow.AddHours(-3),
            UpgradesScannedAt = DateTimeOffset.UtcNow.AddHours(-3)
        };
        _cacheStore.LoadAsync(Arg.Any<CancellationToken>()).Returns(cached);

        var service = new PackageCacheService(_packageRepo, _cacheStore, _settingsRepo, NullLogger<PackageCacheService>.Instance);

        // Act
        var snapshot = await service.GetCachedSnapshotAsync();

        // Assert
        snapshot.InstalledPackages.Should().HaveCount(1);
        snapshot.UpgradablePackages.Should().HaveCount(1);
        snapshot.InstalledPackagesScannedAt.Should().Be(cached.InstalledPackagesScannedAt);
        await _packageRepo.DidNotReceive().GetInstalledPackagesAsync(Arg.Any<CancellationToken>());
        await _packageRepo.DidNotReceive().GetAvailableUpgradesAsync(Arg.Any<CancellationToken>());
    }
}
