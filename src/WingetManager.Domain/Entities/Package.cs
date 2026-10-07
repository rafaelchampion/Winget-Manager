namespace WingetManager.Domain.Entities;

public sealed record Package
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string InstalledVersion { get; init; }
    public string? AvailableVersion { get; init; }
    public string? Source { get; init; }

    public bool HasUpdate => !string.IsNullOrWhiteSpace(AvailableVersion) 
                             && !string.Equals(AvailableVersion, InstalledVersion, StringComparison.OrdinalIgnoreCase);
}
