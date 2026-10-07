using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Application.UseCases;

public sealed class ManageVersionPinsUseCase(
    ISettingsRepository settingsRepository,
    ILogger<ManageVersionPinsUseCase> logger)
{
    public async Task<IReadOnlyList<VersionPin>> GetPinsAsync(CancellationToken ct = default)
    {
        var settings = await settingsRepository.LoadAsync(ct);
        return settings.PinnedVersions;
    }

    public async Task PinVersionAsync(string packageId, string version, string? reason = null, CancellationToken ct = default)
    {
        var settings = await settingsRepository.LoadAsync(ct);
        settings.PinnedVersions.RemoveAll(p => string.Equals(p.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
        settings.PinnedVersions.Add(new VersionPin(packageId, version, reason));
        await settingsRepository.SaveAsync(settings, ct);
        logger.LogInformation("Pinned package {PackageId} to version {Version}", packageId, version);
    }

    public async Task UnpinVersionAsync(string packageId, CancellationToken ct = default)
    {
        var settings = await settingsRepository.LoadAsync(ct);
        if (settings.PinnedVersions.RemoveAll(p => string.Equals(p.PackageId, packageId, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            await settingsRepository.SaveAsync(settings, ct);
            logger.LogInformation("Unpinned package {PackageId}", packageId);
        }
    }
}
