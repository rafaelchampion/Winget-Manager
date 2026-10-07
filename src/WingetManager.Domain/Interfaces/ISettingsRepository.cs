using WingetManager.Domain.Entities;

namespace WingetManager.Domain.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
