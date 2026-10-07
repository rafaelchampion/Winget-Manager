using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WingetManager.Domain.Entities;
using WingetManager.Infrastructure.Persistence;
using Xunit;

namespace WingetManager.Infrastructure.Tests;

public class JsonPackageCacheStoreTests
{
    [Fact]
    public async Task SaveAndLoad_ShouldPersistCacheAccurately()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"winget_cache_test_{Guid.NewGuid():N}.json");
        var store = new JsonPackageCacheStore(NullLogger<JsonPackageCacheStore>.Instance, tempFile);

        var now = DateTimeOffset.UtcNow;
        var cacheData = new PackageCacheData
        {
            InstalledPackages =
            [
                new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0", AvailableVersion = "2.45.0", Source = "winget" }
            ],
            InstalledPackagesScannedAt = now,
            UpgradablePackages =
            [
                new() { Id = "Git.Git", Name = "Git", InstalledVersion = "2.44.0", AvailableVersion = "2.45.0", Source = "winget" }
            ],
            UpgradesScannedAt = now
        };

        try
        {
            // Act
            await store.SaveAsync(cacheData);
            var loaded = await store.LoadAsync();

            // Assert
            loaded.InstalledPackages.Should().HaveCount(1);
            loaded.InstalledPackages[0].Id.Should().Be("Git.Git");
            loaded.InstalledPackagesScannedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(2));
            loaded.UpgradablePackages.Should().HaveCount(1);
            loaded.UpgradablePackages[0].Id.Should().Be("Git.Git");
            loaded.UpgradesScannedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(2));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task Load_WhenFileNotFound_ShouldReturnEmptyCache()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"non_existent_{Guid.NewGuid():N}.json");
        var store = new JsonPackageCacheStore(NullLogger<JsonPackageCacheStore>.Instance, tempFile);

        // Act
        var loaded = await store.LoadAsync();

        // Assert
        loaded.InstalledPackages.Should().BeEmpty();
        loaded.InstalledPackagesScannedAt.Should().BeNull();
        loaded.UpgradablePackages.Should().BeEmpty();
        loaded.UpgradesScannedAt.Should().BeNull();
    }
}
