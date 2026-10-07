using WingetManager.Domain.Entities;

namespace WingetManager.Domain.Interfaces;

public interface IElevationService
{
    bool IsElevated { get; }
    Task<PackageUpgradeResult> RunElevatedUpgradeAsync(
        string packageId, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default);
}
