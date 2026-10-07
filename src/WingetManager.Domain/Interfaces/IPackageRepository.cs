using WingetManager.Domain.Entities;

namespace WingetManager.Domain.Interfaces;

public interface IPackageRepository
{
    Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Package>> GetAvailableUpgradesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Package>> SearchPackagesAsync(string query, CancellationToken ct = default);
    Task<PackageUpgradeResult> UpgradePackageAsync(string packageId, IProgress<UpgradeProgress>? progress = null, CancellationToken ct = default);
    Task<PackageUpgradeResult> InstallPackageAsync(string packageId, string? version = null, IProgress<UpgradeProgress>? progress = null, CancellationToken ct = default);
}
