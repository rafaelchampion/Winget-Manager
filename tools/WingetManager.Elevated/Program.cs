using System.Diagnostics;

namespace WingetManager.Elevated;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("WingetManager Elevated Helper");
            Console.WriteLine("Usage: WingetManager.Elevated.exe <command> <packageId> [args...]");
            return 1;
        }

        string command = args[0].ToLowerInvariant();
        string packageId = args[1];

        Console.WriteLine($"[Elevated Helper] Processing {command} for package: {packageId}");

        string wingetArgs = command switch
        {
            "upgrade" => $"upgrade --id \"{packageId}\" --exact --accept-source-agreements --accept-package-agreements",
            "install" => $"install --id \"{packageId}\" --exact --accept-source-agreements --accept-package-agreements",
            _ => throw new ArgumentException($"Unknown command '{command}'")
        };

        var psi = new ProcessStartInfo
        {
            FileName = "winget",
            Arguments = wingetArgs,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) Console.Error.WriteLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        Console.WriteLine($"[Elevated Helper] Finished with exit code: {process.ExitCode}");
        return process.ExitCode;
    }
}
