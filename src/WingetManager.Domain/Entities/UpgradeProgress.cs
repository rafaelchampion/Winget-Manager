namespace WingetManager.Domain.Entities;

public enum UpgradeState
{
    Queued,
    Downloading,
    Installing,
    Completed,
    Failed,
    Cancelled
}

public sealed record UpgradeProgress(
    string PackageId,
    UpgradeState State,
    double ProgressPercentage = 0,
    string? StatusMessage = null);
