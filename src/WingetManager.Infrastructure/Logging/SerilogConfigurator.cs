using Serilog;

namespace WingetManager.Infrastructure.Logging;

public static class SerilogConfigurator
{
    public static void ConfigureLogging()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var logFolder = Path.Combine(appData, "WingetManager", "Logs");
        Directory.CreateDirectory(logFolder);

        var logFilePath = Path.Combine(logFolder, "winget-manager-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
