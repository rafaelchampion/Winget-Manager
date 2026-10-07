using Microsoft.Extensions.Logging;
using WingetManager.Application.Services;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Application.UseCases;

public sealed class ScanForUpgradesUseCase(
    IPackageCacheService cacheService,
    ISettingsRepository settingsRepository,
    ILogger<ScanForUpgradesUseCase> logger)
{
    public async Task<IReadOnlyList<Package>> ExecuteAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        logger.LogInformation("Scanning for available package upgrades (forceRefresh: {ForceRefresh})...", forceRefresh);

        var settings = await settingsRepository.LoadAsync(ct);
        var excludedSet = new HashSet<string>(settings.ExcludedPackageIds, StringComparer.OrdinalIgnoreCase);
        var pinnedLookup = settings.PinnedVersions.ToDictionary(
            p => p.PackageId, 
            p => p.PinnedVersion, 
            StringComparer.OrdinalIgnoreCase);

        var allUpgrades = await cacheService.GetAvailableUpgradesAsync(forceRefresh, ct);

        var filtered = allUpgrades.Where(pkg =>
        {
            if (excludedSet.Contains(pkg.Id))
            {
                logger.LogDebug("Package {PackageId} excluded by user preference", pkg.Id);
                return false;
            }

            if (pinnedLookup.TryGetValue(pkg.Id, out var pinnedVer))
            {
                if (string.Equals(pinnedVer, pkg.InstalledVersion, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogDebug("Package {PackageId} pinned to installed version {Version}", pkg.Id, pinnedVer);
                    return false;
                }
            }

            return true;
        }).ToList();

        logger.LogInformation("Found {Count} upgradable packages (after exclusions and pins)", filtered.Count);
        return filtered;
    }
}
