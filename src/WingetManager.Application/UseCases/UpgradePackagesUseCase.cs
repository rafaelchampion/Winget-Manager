using Microsoft.Extensions.Logging;
using WingetManager.Application.Services;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Application.UseCases;

public record UpgradePackageTask(string PackageId, CancellationToken CancellationToken = default);

public sealed class UpgradePackagesUseCase(
    IPackageRepository packageRepository,
    IElevationService elevationService,
    ISettingsRepository settingsRepository,
    IPackageCacheService cacheService,
    ILogger<UpgradePackagesUseCase> logger)
{
    public Task<IReadOnlyList<PackageUpgradeResult>> ExecuteAsync(
        IReadOnlyList<string> packageIds,
        IProgress<UpgradeProgress>? progress = null,
        CancellationToken ct = default)
    {
        var tasks = packageIds.Select(id => new UpgradePackageTask(id, ct)).ToList();
        return ExecuteAsync(tasks, progress, ct);
    }

    public async Task<IReadOnlyList<PackageUpgradeResult>> ExecuteAsync(
        IReadOnlyList<UpgradePackageTask> upgradeTasks,
        IProgress<UpgradeProgress>? progress = null,
        CancellationToken overallCt = default)
    {
        if (upgradeTasks == null || upgradeTasks.Count == 0)
        {
            return [];
        }

        var settings = await settingsRepository.LoadAsync(overallCt);
        var maxConcurrency = Math.Max(1, settings.MaxConcurrentUpgrades);
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        logger.LogInformation("Starting simultaneous upgrade of {Count} packages with concurrency limit {Limit}", 
            upgradeTasks.Count, maxConcurrency);

        var tasks = upgradeTasks.Select(async task =>
        {
            var packageId = task.PackageId;
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(task.CancellationToken, overallCt);
            var ct = linkedCts.Token;

            progress?.Report(new UpgradeProgress(packageId, UpgradeState.Queued, 0, "Queued for upgrade..."));

            bool semaphoreAcquired = false;
            try
            {
                await semaphore.WaitAsync(ct);
                semaphoreAcquired = true;

                ct.ThrowIfCancellationRequested();
                logger.LogInformation("Upgrading package {PackageId}...", packageId);
                
                var result = await packageRepository.UpgradePackageAsync(packageId, progress, ct);

                // If upgrade failed due to permission/elevation issues, try elevated helper
                if (!result.Success && NeedsElevation(result.ErrorMessage))
                {
                    logger.LogWarning("Package {PackageId} upgrade requested elevation. Invoking elevation service...", packageId);
                    progress?.Report(new UpgradeProgress(packageId, UpgradeState.Installing, 50, "Requesting administrative permissions..."));
                    result = await elevationService.RunElevatedUpgradeAsync(packageId, progress, ct);
                }

                if (result.Success)
                {
                    await cacheService.MarkPackageUpgradedAsync(packageId, result.NewVersion, ct);
                    progress?.Report(new UpgradeProgress(packageId, UpgradeState.Completed, 100, "Upgrade completed successfully."));
                }
                else
                {
                    progress?.Report(new UpgradeProgress(packageId, UpgradeState.Failed, 100, result.ErrorMessage ?? "Upgrade failed."));
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Upgrade for {PackageId} was cancelled.", packageId);
                progress?.Report(new UpgradeProgress(packageId, UpgradeState.Cancelled, 0, "Upgrade cancelled."));
                return new PackageUpgradeResult(packageId, false, "Cancelled by user");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error upgrading package {PackageId}", packageId);
                progress?.Report(new UpgradeProgress(packageId, UpgradeState.Failed, 100, ex.Message));
                return new PackageUpgradeResult(packageId, false, ex.Message);
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    semaphore.Release();
                }
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
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
