using WingetManager.Domain.Entities;

namespace WingetManager.Domain.Interfaces;

public interface IPackageCacheStore
{
    Task<PackageCacheData> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(PackageCacheData data, CancellationToken ct = default);
}
