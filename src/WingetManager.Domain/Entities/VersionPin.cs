namespace WingetManager.Domain.Entities;

public sealed record VersionPin(
    string PackageId,
    string PinnedVersion,
    string? Reason = null);
