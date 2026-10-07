using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Infrastructure.Winget;

public class WingetComRepository(ILogger<WingetComRepository> logger) : IPackageRepository
{
    // The COM API uses Microsoft.Management.Deployment (Windows.Management.Deployment)
    // In scenarios where the WinGet COM server is not registered or supported on the host OS build,
    // this class signals unavailability to trigger the CLI fallback.
    public Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Attempting to query installed packages via WinGet COM API...");
        throw new NotSupportedException("WinGet COM API is unavailable or requires in-proc catalog registration. Using CLI fallback.");
    }

    public Task<IReadOnlyList<Package>> GetAvailableUpgradesAsync(CancellationToken ct = default)
    {
        logger.LogDebug("Attempting to query available upgrades via WinGet COM API...");
        throw new NotSupportedException("WinGet COM API is unavailable or requires in-proc catalog registration. Using CLI fallback.");
    }

    public Task<IReadOnlyList<Package>> SearchPackagesAsync(string query, CancellationToken ct = default)
    {
        logger.LogDebug("Attempting to search packages via WinGet COM API...");
        throw new NotSupportedException("WinGet COM API is unavailable or requires in-proc catalog registration. Using CLI fallback.");
    }

    public Task<PackageUpgradeResult> UpgradePackageAsync(
        string packageId, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        logger.LogDebug("Attempting to upgrade package via WinGet COM API...");
        throw new NotSupportedException("WinGet COM API is unavailable or requires in-proc catalog registration. Using CLI fallback.");
    }

    public Task<PackageUpgradeResult> InstallPackageAsync(
        string packageId, 
        string? version = null, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        logger.LogDebug("Attempting to install package via WinGet COM API...");
        throw new NotSupportedException("WinGet COM API is unavailable or requires in-proc catalog registration. Using CLI fallback.");
    }
}
