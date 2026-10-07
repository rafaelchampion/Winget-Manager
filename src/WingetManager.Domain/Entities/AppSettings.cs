namespace WingetManager.Domain.Entities;

public enum AppTheme
{
    System,
    Light,
    Dark
}

public sealed class AppSettings
{
    public int MaxConcurrentUpgrades { get; set; } = 3;
    public bool AutoScanOnStartup { get; set; } = true;
    public int CacheTtlMinutes { get; set; } = 15;
    public List<string> ExcludedPackageIds { get; set; } = [];
    public List<VersionPin> PinnedVersions { get; set; } = [];
    public AppTheme PreferredTheme { get; set; } = AppTheme.System;
    public string Language { get; set; } = "en-US";
}
