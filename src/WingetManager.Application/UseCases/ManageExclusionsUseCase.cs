using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Application.UseCases;

public sealed class ManageExclusionsUseCase(
    ISettingsRepository settingsRepository,
    ILogger<ManageExclusionsUseCase> logger)
{
    public async Task<IReadOnlyList<string>> GetExclusionsAsync(CancellationToken ct = default)
    {
        var settings = await settingsRepository.LoadAsync(ct);
        return settings.ExcludedPackageIds;
    }

    public async Task AddExclusionAsync(string packageId, CancellationToken ct = default)
    {
        var settings = await settingsRepository.LoadAsync(ct);
        if (!settings.ExcludedPackageIds.Contains(packageId, StringComparer.OrdinalIgnoreCase))
        {
            settings.ExcludedPackageIds.Add(packageId);
            await settingsRepository.SaveAsync(settings, ct);
            logger.LogInformation("Added package {PackageId} to exclusion list", packageId);
        }
    }

    public async Task RemoveExclusionAsync(string packageId, CancellationToken ct = default)
    {
        var settings = await settingsRepository.LoadAsync(ct);
        if (settings.ExcludedPackageIds.RemoveAll(id => string.Equals(id, packageId, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            await settingsRepository.SaveAsync(settings, ct);
            logger.LogInformation("Removed package {PackageId} from exclusion list", packageId);
        }
    }
}
