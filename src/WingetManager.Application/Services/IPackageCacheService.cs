using WingetManager.Domain.Entities;

namespace WingetManager.Application.Services;

public record PackageCacheSnapshot(
    IReadOnlyList<Package> InstalledPackages,
    IReadOnlyList<Package> UpgradablePackages,
    DateTimeOffset? InstalledPackagesScannedAt,
    DateTimeOffset? UpgradesScannedAt);

public interface IPackageCacheService
{
    DateTimeOffset? InstalledPackagesScannedAt { get; }
    DateTimeOffset? UpgradesScannedAt { get; }

    Task<PackageCacheSnapshot> GetCachedSnapshotAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(bool forceRefresh = false, CancellationToken ct = default);
    Task<IReadOnlyList<Package>> GetAvailableUpgradesAsync(bool forceRefresh = false, CancellationToken ct = default);

    Task InvalidateCacheAsync(CancellationToken ct = default);
    Task InvalidateUpgradesAsync(CancellationToken ct = default);
    Task MarkPackageUpgradedAsync(string packageId, string? newVersion = null, CancellationToken ct = default);
}
