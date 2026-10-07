using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using WingetManager.Domain.Entities;

namespace WingetManager.UI.ViewModels;

public partial class UpgradeProgressItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _packageId = string.Empty;

    [ObservableProperty]
    private string _packageName = string.Empty;

    [ObservableProperty]
    private UpgradeState _state = UpgradeState.Queued;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isIndeterminate = true;

    [ObservableProperty]
    private bool _canCancel = true;

    [ObservableProperty]
    private bool _canRetry;

    [ObservableProperty]
    private bool _isFinished;

    [ObservableProperty]
    private Visibility _cancelVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility _dismissVisibility = Visibility.Collapsed;

    public CancellationTokenSource CancellationTokenSource { get; set; } = new();

    public void UpdateProgress(UpgradeProgress progress)
    {
        State = progress.State;
        ProgressPercentage = progress.ProgressPercentage;
        StatusMessage = progress.StatusMessage ?? progress.State.ToString();
        IsIndeterminate = progress.ProgressPercentage <= 0 && 
            progress.State != UpgradeState.Queued && 
            progress.State != UpgradeState.Completed && 
            progress.State != UpgradeState.Failed && 
            progress.State != UpgradeState.Cancelled;
        CanCancel = progress.State == UpgradeState.Queued || 
            progress.State == UpgradeState.Downloading || 
            progress.State == UpgradeState.Installing;
        CanRetry = progress.State == UpgradeState.Failed || progress.State == UpgradeState.Cancelled;
        IsFinished = progress.State == UpgradeState.Completed || progress.State == UpgradeState.Failed || progress.State == UpgradeState.Cancelled;

        CancelVisibility = CanCancel ? Visibility.Visible : Visibility.Collapsed;
        DismissVisibility = IsFinished ? Visibility.Visible : Visibility.Collapsed;
    }
}
