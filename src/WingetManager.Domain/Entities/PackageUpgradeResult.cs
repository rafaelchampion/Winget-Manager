namespace WingetManager.Domain.Entities;

public sealed record PackageUpgradeResult(
    string PackageId,
    bool Success,
    string? ErrorMessage = null,
    string? OldVersion = null,
    string? NewVersion = null);
