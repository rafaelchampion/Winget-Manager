namespace WingetManager.Domain.Entities;

public sealed class PackageCacheData
{
    public List<Package> InstalledPackages { get; set; } = [];
    public DateTimeOffset? InstalledPackagesScannedAt { get; set; }
    public List<Package> UpgradablePackages { get; set; } = [];
    public DateTimeOffset? UpgradesScannedAt { get; set; }
}
