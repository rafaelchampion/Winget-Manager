using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Infrastructure.Winget;

public class WingetCliRepository(ILogger<WingetCliRepository> logger) : IPackageRepository
{
    public virtual async Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Executing 'winget list'...");
        var (exitCode, stdout, stderr) = await RunWingetProcessAsync("list", ct: ct);
        if (exitCode != 0)
        {
            logger.LogWarning("winget list returned exit code {ExitCode}: {Error}", exitCode, stderr);
        }

        return WingetCliParser.ParseUpgradesTable(stdout);
    }

    public virtual async Task<IReadOnlyList<Package>> GetAvailableUpgradesAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Executing 'winget upgrade'...");
        var (exitCode, stdout, stderr) = await RunWingetProcessAsync("upgrade --include-unknown", ct: ct);
        if (exitCode != 0 && !stdout.Contains("No installed package found", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("winget upgrade returned exit code {ExitCode}: {Error}", exitCode, stderr);
        }

        return WingetCliParser.ParseUpgradesTable(stdout);
    }

    public virtual async Task<IReadOnlyList<Package>> SearchPackagesAsync(string query, CancellationToken ct = default)
    {
        logger.LogInformation("Executing 'winget search {Query}'...", query);
        var (exitCode, stdout, stderr) = await RunWingetProcessAsync($"search \"{query}\"", ct: ct);
        if (exitCode != 0)
        {
            logger.LogWarning("winget search returned exit code {ExitCode}: {Error}", exitCode, stderr);
        }

        return WingetCliParser.ParseSearchTable(stdout);
    }

    public virtual async Task<PackageUpgradeResult> UpgradePackageAsync(
        string packageId, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        logger.LogInformation("Executing 'winget upgrade --id {PackageId}'...", packageId);
        progress?.Report(new UpgradeProgress(packageId, UpgradeState.Downloading, 10, "Starting package upgrade..."));

        var args = $"upgrade --id \"{packageId}\" --exact --accept-source-agreements --accept-package-agreements";

        var (exitCode, stdout, stderr) = await RunWingetProcessAsync(args, line =>
        {
            var percent = WingetCliParser.ExtractProgressPercentage(line);
            if (percent.HasValue)
            {
                var state = percent.Value >= 90 ? UpgradeState.Installing : UpgradeState.Downloading;
                progress?.Report(new UpgradeProgress(packageId, state, percent.Value, line.Trim()));
            }
            else if (line.Contains("installer", StringComparison.OrdinalIgnoreCase) || 
                     line.Contains("installing", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report(new UpgradeProgress(packageId, UpgradeState.Installing, 70, line.Trim()));
            }
        }, ct);

        bool success = exitCode == 0 || stdout.Contains("Successfully installed", StringComparison.OrdinalIgnoreCase)
                                     || stdout.Contains("Instalação bem-sucedida", StringComparison.OrdinalIgnoreCase);

        string? error = null;
        if (!success)
        {
            error = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
        }

        return new PackageUpgradeResult(packageId, success, error);
    }

    public virtual async Task<PackageUpgradeResult> InstallPackageAsync(
        string packageId, 
        string? version = null, 
        IProgress<UpgradeProgress>? progress = null, 
        CancellationToken ct = default)
    {
        logger.LogInformation("Executing 'winget install --id {PackageId}'...", packageId);
        progress?.Report(new UpgradeProgress(packageId, UpgradeState.Downloading, 10, "Starting package installation..."));

        var args = $"install --id \"{packageId}\" --exact --accept-source-agreements --accept-package-agreements";
        if (!string.IsNullOrWhiteSpace(version))
        {
            args += $" --version \"{version}\"";
        }

        var (exitCode, stdout, stderr) = await RunWingetProcessAsync(args, line =>
        {
            var percent = WingetCliParser.ExtractProgressPercentage(line);
            if (percent.HasValue)
            {
                var state = percent.Value >= 90 ? UpgradeState.Installing : UpgradeState.Downloading;
                progress?.Report(new UpgradeProgress(packageId, state, percent.Value, line.Trim()));
            }
            else if (line.Contains("installing", StringComparison.OrdinalIgnoreCase) || 
                     line.Contains("instalando", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Report(new UpgradeProgress(packageId, UpgradeState.Installing, 70, line.Trim()));
            }
        }, ct);

        bool success = exitCode == 0 || stdout.Contains("Successfully installed", StringComparison.OrdinalIgnoreCase)
                                     || stdout.Contains("Instalação bem-sucedida", StringComparison.OrdinalIgnoreCase);

        string? error = null;
        if (!success)
        {
            error = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
        }

        return new PackageUpgradeResult(packageId, success, error);
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunWingetProcessAsync(
        string arguments, 
        Action<string>? onStdOutLine = null, 
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

        await process.WaitForExitAsync(ct);

        return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
    }
}
