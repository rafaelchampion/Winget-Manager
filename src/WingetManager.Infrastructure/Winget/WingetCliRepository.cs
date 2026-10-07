using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Infrastructure.Winget;

public class WingetCliRepository(ILogger<WingetCliRepository> logger) : IPackageRepository
{
    private static readonly TimeSpan DefaultQueryTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan DefaultInstallTimeout = TimeSpan.FromMinutes(20);

    public virtual async Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Executing 'winget list'...");
        try
        {
            var (exitCode, stdout, stderr) = await RunWingetProcessAsync("list --accept-source-agreements", timeout: DefaultQueryTimeout, ct: ct);
            if (exitCode != 0)
            {
                logger.LogWarning("winget list returned exit code {ExitCode}: {Error}", exitCode, stderr);
            }

            return WingetCliParser.ParseUpgradesTable(stdout);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to execute 'winget list'");
            return [];
        }
    }

    public virtual async Task<IReadOnlyList<Package>> GetAvailableUpgradesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Executing 'winget upgrade'...");
        try
        {
            var (exitCode, stdout, stderr) = await RunWingetProcessAsync("upgrade --include-unknown --accept-source-agreements", timeout: DefaultQueryTimeout, ct: ct);
            if (exitCode != 0 && !stdout.Contains("No installed package found", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("winget upgrade returned exit code {ExitCode}: {Error}", exitCode, stderr);
            }

            return WingetCliParser.ParseUpgradesTable(stdout);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to execute 'winget upgrade'");
            return [];
        }
    }

    public virtual async Task<IReadOnlyList<Package>> SearchPackagesAsync(string query, CancellationToken ct = default)
    {
        var cleanQuery = WingetCliParser.SanitizeSearchQuery(query);
        if (string.IsNullOrWhiteSpace(cleanQuery))
        {
            return [];
        }

        logger.LogInformation("Executing 'winget search {Query}'...", cleanQuery);
        try
        {
            var (exitCode, stdout, stderr) = await RunWingetProcessAsync($"search \"{cleanQuery}\" --accept-source-agreements", timeout: DefaultQueryTimeout, ct: ct);
            if (exitCode != 0)
            {
                logger.LogWarning("winget search returned exit code {ExitCode}: {Error}", exitCode, stderr);
            }

            return WingetCliParser.ParseSearchTable(stdout);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to execute 'winget search'");
            return [];
        }
    }

    public virtual async Task<PackageUpgradeResult> UpgradePackageAsync(
        string packageId, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        var cleanId = WingetCliParser.SanitizePackageId(packageId);
        if (string.IsNullOrWhiteSpace(cleanId))
        {
            return new PackageUpgradeResult(packageId, false, "Invalid package ID.");
        }

        logger.LogInformation("Executing 'winget upgrade --id {PackageId}'...", cleanId);
        progress?.Report(new UpgradeProgress(cleanId, UpgradeState.Downloading, 10, "Starting package upgrade..."));

        var args = $"upgrade --id \"{cleanId}\" --exact --accept-source-agreements --accept-package-agreements --disable-interactivity";

        try
        {
            var (exitCode, stdout, stderr) = await RunWingetProcessAsync(args, line =>
            {
                var percent = WingetCliParser.ExtractProgressPercentage(line);
                if (percent.HasValue)
                {
                    var state = percent.Value >= 90 ? UpgradeState.Installing : UpgradeState.Downloading;
                    progress?.Report(new UpgradeProgress(cleanId, state, percent.Value, line.Trim()));
                }
                else if (line.Contains("installer", StringComparison.OrdinalIgnoreCase) || 
                         line.Contains("installing", StringComparison.OrdinalIgnoreCase) ||
                         line.Contains("instalando", StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report(new UpgradeProgress(cleanId, UpgradeState.Installing, 70, line.Trim()));
                }
            }, timeout: DefaultInstallTimeout, ct: ct);

            var (success, error) = WingetCliParser.InterpretExitCode(exitCode, stdout, stderr);
            return new PackageUpgradeResult(cleanId, success, error);
        }
        catch (TimeoutException tex)
        {
            logger.LogError(tex, "Timeout upgrading package {PackageId}", cleanId);
            return new PackageUpgradeResult(cleanId, false, tex.Message);
        }
    }

    public virtual async Task<PackageUpgradeResult> InstallPackageAsync(
        string packageId, 
        string? version = null, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        var cleanId = WingetCliParser.SanitizePackageId(packageId);
        if (string.IsNullOrWhiteSpace(cleanId))
        {
            return new PackageUpgradeResult(packageId, false, "Invalid package ID.");
        }

        logger.LogInformation("Executing 'winget install --id {PackageId}'...", cleanId);
        progress?.Report(new UpgradeProgress(cleanId, UpgradeState.Downloading, 10, "Starting package installation..."));

        var args = $"install --id \"{cleanId}\" --exact --accept-source-agreements --accept-package-agreements --disable-interactivity";
        if (!string.IsNullOrWhiteSpace(version))
        {
            var cleanVersion = WingetCliParser.SanitizeSearchQuery(version);
            if (!string.IsNullOrWhiteSpace(cleanVersion))
            {
                args += $" --version \"{cleanVersion}\"";
            }
        }

        try
        {
            var (exitCode, stdout, stderr) = await RunWingetProcessAsync(args, line =>
            {
                var percent = WingetCliParser.ExtractProgressPercentage(line);
                if (percent.HasValue)
                {
                    var state = percent.Value >= 90 ? UpgradeState.Installing : UpgradeState.Downloading;
                    progress?.Report(new UpgradeProgress(cleanId, state, percent.Value, line.Trim()));
                }
                else if (line.Contains("installing", StringComparison.OrdinalIgnoreCase) || 
                         line.Contains("instalando", StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report(new UpgradeProgress(cleanId, UpgradeState.Installing, 70, line.Trim()));
                }
            }, timeout: DefaultInstallTimeout, ct: ct);

            var (success, error) = WingetCliParser.InterpretExitCode(exitCode, stdout, stderr);
            return new PackageUpgradeResult(cleanId, success, error);
        }
        catch (TimeoutException tex)
        {
            logger.LogError(tex, "Timeout installing package {PackageId}", cleanId);
            return new PackageUpgradeResult(cleanId, false, tex.Message);
        }
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunWingetProcessAsync(
        string arguments, 
        Action<string>? onStdOutLine = null, 
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "winget",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stdoutBuilder.AppendLine(e.Data);
                onStdOutLine?.Invoke(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                stderrBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (timeout.HasValue)
        {
            linkedCts.CancelAfter(timeout.Value);
        }

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
            return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best effort process cleanup
            }

            if (ct.IsCancellationRequested)
            {
                throw;
            }

            throw new TimeoutException($"The winget process timed out after {timeout?.TotalSeconds}s.");
        }
    }
}
