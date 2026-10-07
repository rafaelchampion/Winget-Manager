using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Infrastructure.Persistence;

public sealed class JsonPackageCacheStore : IPackageCacheStore
{
    private readonly ILogger<JsonPackageCacheStore> _logger;
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonPackageCacheStore(ILogger<JsonPackageCacheStore> logger, string? customFilePath = null)
    {
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(customFilePath))
        {
            _filePath = customFilePath;
        }
        else
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(appData, "WingetManager");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "package_cache.json");
        }
    }

    public async Task<PackageCacheData> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("Package cache file not found at {Path}, returning empty cache", _filePath);
                return new PackageCacheData();
            }

            var json = await File.ReadAllTextAsync(_filePath, ct);
            var data = JsonSerializer.Deserialize<PackageCacheData>(json, JsonOptions);
            return data ?? new PackageCacheData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load package cache from {Path}, returning empty cache", _filePath);
            return new PackageCacheData();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(PackageCacheData data, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json, ct);
            _logger.LogInformation("Successfully persisted package cache to {Path}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save package cache to {Path}", _filePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}
