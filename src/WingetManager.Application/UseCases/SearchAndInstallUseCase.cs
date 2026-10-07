using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Application.UseCases;

public sealed class SearchAndInstallUseCase(
    IPackageRepository packageRepository,
    IElevationService elevationService,
    ILogger<SearchAndInstallUseCase> logger)
{
    public async Task<IReadOnlyList<Package>> SearchAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        logger.LogInformation("Searching packages with query: {Query}", query);
        return await packageRepository.SearchPackagesAsync(query, ct);
    }

    public async Task<PackageUpgradeResult> InstallAsync(
        string packageId, 
        string? version = null, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        logger.LogInformation("Installing package {PackageId} version {Version}...", packageId, version ?? "latest");
        progress?.Report(new UpgradeProgress(packageId, UpgradeState.Downloading, 10, "Starting installation..."));

        var result = await packageRepository.InstallPackageAsync(packageId, version, progress, ct);

        if (!result.Success && NeedsElevation(result.ErrorMessage))
        {
            logger.LogWarning("Installation of {PackageId} requires elevation. Running elevated helper...", packageId);
            progress?.Report(new UpgradeProgress(packageId, UpgradeState.Installing, 50, "Requesting administrative privileges..."));
            result = await elevationService.RunElevatedUpgradeAsync(packageId, progress, ct);
        }

        if (result.Success)
        {
            progress?.Report(new UpgradeProgress(packageId, UpgradeState.Completed, 100, "Package installed successfully."));
        }
        else
        {
            progress?.Report(new UpgradeProgress(packageId, UpgradeState.Failed, 100, result.ErrorMessage ?? "Installation failed."));
        }

        return result;
    }

    private static bool NeedsElevation(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage)) return false;
        
        return errorMessage.Contains("elevation", StringComparison.OrdinalIgnoreCase) ||
               errorMessage.Contains("access is denied", StringComparison.OrdinalIgnoreCase) ||
               errorMessage.Contains("administrator", StringComparison.OrdinalIgnoreCase) ||
               errorMessage.Contains("0x80070005", StringComparison.OrdinalIgnoreCase);
    }
}
