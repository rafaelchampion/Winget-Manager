using CommunityToolkit.Mvvm.ComponentModel;
using WingetManager.Domain.Entities;

namespace WingetManager.UI.ViewModels;

public partial class PackageItemViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Id))]
    [NotifyPropertyChangedFor(nameof(Name))]
    [NotifyPropertyChangedFor(nameof(InstalledVersion))]
    [NotifyPropertyChangedFor(nameof(AvailableVersion))]
    [NotifyPropertyChangedFor(nameof(Source))]
    [NotifyPropertyChangedFor(nameof(HasSource))]
    [NotifyPropertyChangedFor(nameof(HasUpdate))]
    [NotifyPropertyChangedFor(nameof(Initials))]
    private Package _item;

    public PackageItemViewModel(Package package, bool isSelected = false)
    {
        _item = package;
        _isSelected = isSelected;
    }

    public string Id => Item.Id;
    public string Name => Item.Name;
    public string InstalledVersion => Item.InstalledVersion;
    public string? AvailableVersion => Item.AvailableVersion;
    public string? Source => Item.Source;
    public bool HasSource => !string.IsNullOrWhiteSpace(Item.Source);
    public bool HasUpdate => Item.HasUpdate;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isPinned;

    [ObservableProperty]
    private bool _isExcluded;

    [ObservableProperty]
    private bool _isDetailsExpanded;

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name)) return "?";
            var parts = Name.Split([' ', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();
            }
            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ToggleDetails()
    {
        IsDetailsExpanded = !IsDetailsExpanded;
    }

    // Per-package upgrade progress (synchronized with UpgradeQueueViewModel)

    [ObservableProperty]
    private bool _hasUpgradeProgress;

    [ObservableProperty]
    private double _upgradePercentage;

    [ObservableProperty]
    private string _upgradeStatusMessage = string.Empty;

    [ObservableProperty]
    private bool _isUpgradeIndeterminate;

    [ObservableProperty]
    private bool _isUpgradeActive;

    public void UpdateUpgradeProgress(UpgradeProgress progress)
    {
        HasUpgradeProgress = true;
        UpgradePercentage = progress.ProgressPercentage;
        UpgradeStatusMessage = progress.StatusMessage ?? progress.State.ToString();
        IsUpgradeIndeterminate = progress.ProgressPercentage <= 0 &&
            progress.State != UpgradeState.Queued &&
            progress.State != UpgradeState.Completed &&
            progress.State != UpgradeState.Failed &&
            progress.State != UpgradeState.Cancelled;
        IsUpgradeActive = progress.State == UpgradeState.Queued ||
            progress.State == UpgradeState.Downloading ||
            progress.State == UpgradeState.Installing;
    }
}
