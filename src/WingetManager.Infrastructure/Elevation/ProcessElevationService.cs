using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Infrastructure.Elevation;

public sealed class ProcessElevationService(ILogger<ProcessElevationService> logger) : IElevationService
{
    public bool IsElevated
    {
        get
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    public async Task<PackageUpgradeResult> RunElevatedUpgradeAsync(
        string packageId, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        logger.LogInformation("Spawning elevated process for package upgrade {PackageId}...", packageId);
        progress?.Report(new UpgradeProgress(packageId, UpgradeState.Installing, 50, "Awaiting administrative permission (UAC)..."));

        var appDir = AppContext.BaseDirectory;
        var helperExe = Path.Combine(appDir, "WingetManager.Elevated.exe");

        string fileName;
        string arguments;

        if (File.Exists(helperExe))
        {
            fileName = helperExe;
            arguments = $"upgrade \"{packageId}\"";
        }
        else
        {
            // Fallback: spawn winget directly elevated
            fileName = "winget";
            arguments = $"upgrade --id \"{packageId}\" --exact --accept-source-agreements --accept-package-agreements";
        }

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas" // Triggers Windows UAC prompt
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null)
            {
                logger.LogError("Failed to start elevated helper for package {PackageId}", packageId);
                return new PackageUpgradeResult(packageId, false, "Failed to start elevated process.");
            }

            progress?.Report(new UpgradeProgress(packageId, UpgradeState.Installing, 75, "Installing with elevated permissions..."));
            await process.WaitForExitAsync(ct);

            bool success = process.ExitCode == 0;
            logger.LogInformation("Elevated upgrade for {PackageId} exited with code {ExitCode}", packageId, process.ExitCode);

            return new PackageUpgradeResult(packageId, success, success ? null : $"Elevated process exited with code {process.ExitCode}");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED (User cancelled UAC)
        {
            logger.LogWarning("User rejected UAC prompt for package {PackageId}", packageId);
            return new PackageUpgradeResult(packageId, false, "Elevation request was cancelled by user.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run elevated upgrade for {PackageId}", packageId);
            return new PackageUpgradeResult(packageId, false, ex.Message);
        }
    }
}
