using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Infrastructure.Winget;

public sealed class HybridPackageRepository(
    WingetComRepository comRepository,
    WingetCliRepository cliRepository,
    ILogger<HybridPackageRepository> logger) : IPackageRepository
{
    public async Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(CancellationToken ct = default)
    {
        try
        {
            return await comRepository.GetInstalledPackagesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "WinGet COM API unavailable for GetInstalledPackagesAsync. Falling back to CLI.");
            return await cliRepository.GetInstalledPackagesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Package>> GetAvailableUpgradesAsync(CancellationToken ct = default)
    {
        try
        {
            return await comRepository.GetAvailableUpgradesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "WinGet COM API unavailable for GetAvailableUpgradesAsync. Falling back to CLI.");
            return await cliRepository.GetAvailableUpgradesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<Package>> SearchPackagesAsync(string query, CancellationToken ct = default)
    {
        try
        {
            return await comRepository.SearchPackagesAsync(query, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "WinGet COM API unavailable for SearchPackagesAsync. Falling back to CLI.");
            return await cliRepository.SearchPackagesAsync(query, ct);
        }
    }

    public async Task<PackageUpgradeResult> UpgradePackageAsync(
        string packageId, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        try
        {
            return await comRepository.UpgradePackageAsync(packageId, progress, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "WinGet COM API unavailable for UpgradePackageAsync. Falling back to CLI.");
            return await cliRepository.UpgradePackageAsync(packageId, progress, ct);
        }
    }

    public async Task<PackageUpgradeResult> InstallPackageAsync(
        string packageId, 
        string? version = null, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        try
        {
            return await comRepository.InstallPackageAsync(packageId, version, progress, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "WinGet COM API unavailable for InstallPackageAsync. Falling back to CLI.");
            return await cliRepository.InstallPackageAsync(packageId, version, progress, ct);
        }
    }
}
