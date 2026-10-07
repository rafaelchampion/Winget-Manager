using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WingetManager.Domain.Entities;
using WingetManager.Domain.Interfaces;

namespace WingetManager.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private int _maxConcurrentUpgrades = 3;

    [ObservableProperty]
    private bool _autoScanOnStartup = true;

    [ObservableProperty]
    private int _cacheTtlMinutes = 15;

    [ObservableProperty]
    private AppTheme _preferredTheme = AppTheme.System;

    [ObservableProperty]
    private string _language = "en-US";

    [ObservableProperty]
    private ObservableCollection<string> _excludedPackageIds = [];

    [ObservableProperty]
    private ObservableCollection<VersionPin> _pinnedVersions = [];

    [ObservableProperty]
    private string _newExclusionId = string.Empty;

    [ObservableProperty]
    private string _newPinId = string.Empty;

    [ObservableProperty]
    private string _newPinVersion = string.Empty;

    [ObservableProperty]
    private string _newPinReason = string.Empty;

    [ObservableProperty]
    private string _saveMessage = string.Empty;

    public SettingsViewModel(ISettingsRepository settingsRepository, ILogger<SettingsViewModel> logger)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsRepository.LoadAsync();
            MaxConcurrentUpgrades = settings.MaxConcurrentUpgrades;
            AutoScanOnStartup = settings.AutoScanOnStartup;
            CacheTtlMinutes = settings.CacheTtlMinutes;
            PreferredTheme = settings.PreferredTheme;
            Language = settings.Language;

            ExcludedPackageIds = new ObservableCollection<string>(settings.ExcludedPackageIds);
            PinnedVersions = new ObservableCollection<VersionPin>(settings.PinnedVersions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
        }
    }

    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        try
        {
            var settings = new AppSettings
            {
                MaxConcurrentUpgrades = MaxConcurrentUpgrades,
                AutoScanOnStartup = AutoScanOnStartup,
                CacheTtlMinutes = CacheTtlMinutes,
                PreferredTheme = PreferredTheme,
                Language = Language,
                ExcludedPackageIds = [.. ExcludedPackageIds],
                PinnedVersions = [.. PinnedVersions]
            };

            await _settingsRepository.SaveAsync(settings);
            SaveMessage = "Settings saved successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            SaveMessage = "Failed to save settings.";
        }
    }

    [RelayCommand]
    public async Task AddExclusionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewExclusionId)) return;

        var id = NewExclusionId.Trim();
        if (!ExcludedPackageIds.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            ExcludedPackageIds.Add(id);
            NewExclusionId = string.Empty;
            await SaveSettingsAsync();
        }
    }

    [RelayCommand]
    public async Task RemoveExclusionAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (ExcludedPackageIds.Remove(id))
        {
            await SaveSettingsAsync();
        }
    }

    [RelayCommand]
    public async Task AddPinAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPinId) || string.IsNullOrWhiteSpace(NewPinVersion)) return;

        var id = NewPinId.Trim();
        var ver = NewPinVersion.Trim();
        var reason = string.IsNullOrWhiteSpace(NewPinReason) ? null : NewPinReason.Trim();

        var existing = PinnedVersions.FirstOrDefault(p => string.Equals(p.PackageId, id, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            PinnedVersions.Remove(existing);
        }

        PinnedVersions.Add(new VersionPin(id, ver, reason));
        NewPinId = string.Empty;
        NewPinVersion = string.Empty;
        NewPinReason = string.Empty;

        await SaveSettingsAsync();
    }

    [RelayCommand]
    public async Task RemovePinAsync(VersionPin? pin)
    {
        if (pin == null) return;
        if (PinnedVersions.Remove(pin))
        {
            await SaveSettingsAsync();
        }
    }
}
