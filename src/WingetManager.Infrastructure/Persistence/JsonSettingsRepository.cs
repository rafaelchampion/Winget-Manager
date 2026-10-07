using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.Infrastructure.Persistence;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly ILogger<JsonSettingsRepository> _logger;
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSettingsRepository(ILogger<JsonSettingsRepository> logger, string? customFilePath = null)
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
            _filePath = Path.Combine(folder, "settings.json");
        }
    }

    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogInformation("Settings file not found at {Path}, returning default settings", _filePath);
                return new AppSettings();
            }

            var json = await File.ReadAllTextAsync(_filePath, ct);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {Path}, using defaults", _filePath);
            return new AppSettings();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_filePath, json, ct);
            _logger.LogInformation("Successfully saved settings to {Path}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {Path}", _filePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}
