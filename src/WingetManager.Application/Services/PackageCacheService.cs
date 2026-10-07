using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Application.Services;

public sealed class PackageCacheService : IPackageCacheService
{
    private readonly IPackageRepository _packageRepository;
    private readonly IPackageCacheStore _cacheStore;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<PackageCacheService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private PackageCacheData? _cacheData;

    public DateTimeOffset? InstalledPackagesScannedAt => _cacheData?.InstalledPackagesScannedAt;
    public DateTimeOffset? UpgradesScannedAt => _cacheData?.UpgradesScannedAt;

    public PackageCacheService(
        IPackageRepository packageRepository,
        IPackageCacheStore cacheStore,
        ISettingsRepository settingsRepository,
        ILogger<PackageCacheService> logger)
    {
        _packageRepository = packageRepository;
        _cacheStore = cacheStore;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_cacheData == null)
        {
            _logger.LogDebug("Loading package cache from disk store...");
            _cacheData = await _cacheStore.LoadAsync(ct);
        }
    }

    public async Task<PackageCacheSnapshot> GetCachedSnapshotAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await EnsureLoadedAsync(ct);
            return new PackageCacheSnapshot(
                _cacheData!.InstalledPackages.AsReadOnly(),
                _cacheData.UpgradablePackages.AsReadOnly(),
                _cacheData.InstalledPackagesScannedAt,
                _cacheData.UpgradesScannedAt);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await EnsureLoadedAsync(ct);

            var settings = await _settingsRepository.LoadAsync(ct);
            var ttl = TimeSpan.FromMinutes(Math.Max(1, settings.CacheTtlMinutes));

            bool hasData = _cacheData!.InstalledPackages.Count > 0;
            bool isFresh = _cacheData.InstalledPackagesScannedAt.HasValue &&
                           (DateTimeOffset.UtcNow - _cacheData.InstalledPackagesScannedAt.Value) < ttl;

            if (!forceRefresh && hasData && isFresh)
            {
                _logger.LogInformation("Returning {Count} installed packages from cache (scanned at {Time})",
                    _cacheData.InstalledPackages.Count, _cacheData.InstalledPackagesScannedAt);
                return _cacheData.InstalledPackages.AsReadOnly();
            }

            _logger.LogInformation("Scanning installed packages from repository (forceRefresh: {ForceRefresh}, hasData: {HasData}, isFresh: {IsFresh})",
                forceRefresh, hasData, isFresh);

            var packages = await _packageRepository.GetInstalledPackagesAsync(ct);
            _cacheData.InstalledPackages = packages.ToList();
            _cacheData.InstalledPackagesScannedAt = DateTimeOffset.UtcNow;

            await _cacheStore.SaveAsync(_cacheData, ct);
            return _cacheData.InstalledPackages.AsReadOnly();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<Package>> GetAvailableUpgradesAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await EnsureLoadedAsync(ct);

            var settings = await _settingsRepository.LoadAsync(ct);
            var ttl = TimeSpan.FromMinutes(Math.Max(1, settings.CacheTtlMinutes));

            bool hasData = _cacheData!.UpgradablePackages.Count > 0;
            bool isFresh = _cacheData.UpgradesScannedAt.HasValue &&
                           (DateTimeOffset.UtcNow - _cacheData.UpgradesScannedAt.Value) < ttl;

            // If we have scanned before and no upgrades were found, that's also valid cache data within TTL!
            bool hasValidScan = _cacheData.UpgradesScannedAt.HasValue && isFresh;

            if (!forceRefresh && hasValidScan)
            {
                _logger.LogInformation("Returning {Count} upgradable packages from cache (scanned at {Time})",
                    _cacheData.UpgradablePackages.Count, _cacheData.UpgradesScannedAt);
                return _cacheData.UpgradablePackages.AsReadOnly();
            }

            _logger.LogInformation("Scanning upgradable packages from repository (forceRefresh: {ForceRefresh})", forceRefresh);

            var upgrades = await _packageRepository.GetAvailableUpgradesAsync(ct);
            _cacheData.UpgradablePackages = upgrades.ToList();
            _cacheData.UpgradesScannedAt = DateTimeOffset.UtcNow;

            await _cacheStore.SaveAsync(_cacheData, ct);
            return _cacheData.UpgradablePackages.AsReadOnly();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MarkPackageUpgradedAsync(string packageId, string? newVersion = null, CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await EnsureLoadedAsync(ct);

            _cacheData!.UpgradablePackages.RemoveAll(p => string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(newVersion))
            {
                var existingInstalled = _cacheData.InstalledPackages.FirstOrDefault(p =>
                    string.Equals(p.Id, packageId, StringComparison.OrdinalIgnoreCase));

                if (existingInstalled != null)
                {
                    var updated = existingInstalled with
                    {
                        InstalledVersion = newVersion,
                        AvailableVersion = null
                    };
                    int index = _cacheData.InstalledPackages.IndexOf(existingInstalled);
                    _cacheData.InstalledPackages[index] = updated;
                }
            }

            await _cacheStore.SaveAsync(_cacheData, ct);
            _logger.LogInformation("Marked package {PackageId} as upgraded in cache", packageId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task InvalidateCacheAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            _cacheData = new PackageCacheData();
            await _cacheStore.SaveAsync(_cacheData, ct);
            _logger.LogInformation("Package cache invalidated completely");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task InvalidateUpgradesAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct);
        try
        {
            await EnsureLoadedAsync(ct);
            _cacheData!.UpgradablePackages.Clear();
            _cacheData.UpgradesScannedAt = null;
            await _cacheStore.SaveAsync(_cacheData, ct);
            _logger.LogInformation("Package upgrades cache invalidated");
        }
        finally
        {
            _gate.Release();
        }
    }
}
