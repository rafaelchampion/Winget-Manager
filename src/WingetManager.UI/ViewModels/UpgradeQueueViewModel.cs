using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WingetManager.Domain.Entities;

namespace WingetManager.UI.ViewModels;

public partial class UpgradeQueueViewModel : ObservableObject
{
    private DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private ObservableCollection<UpgradeProgressItemViewModel> _items = [];

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _inProgressCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private bool _hasActiveJobs;

    [ObservableProperty]
    private bool _hasItems;

    [ObservableProperty]
    private bool _isPanelExpanded;

    [ObservableProperty]
    private Visibility _queueBarVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _activeJobsVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private Visibility _idleJobsVisibility = Visibility.Visible;

    [ObservableProperty]
    private Visibility _panelDrawerVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string _summaryStatusText = "No active updates";

    [ObservableProperty]
    private double _overallPercentage;

    public event Action<UpgradeProgress>? ProgressReported;

    public void InitializeDispatcher(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    private void RunOnUIThread(Action action)
    {
        if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(() => action());
        }
        else
        {
            action();
        }
    }

    public UpgradeProgressItemViewModel EnqueuePackage(string packageId, string packageName, CancellationTokenSource? cts = null)
    {
        UpgradeProgressItemViewModel result = null!;

        RunOnUIThread(() =>
        {
            var existing = Items.FirstOrDefault(i => string.Equals(i.PackageId, packageId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.CancellationTokenSource = cts ?? new CancellationTokenSource();
                existing.State = UpgradeState.Queued;
                existing.ProgressPercentage = 0;
                existing.StatusMessage = "Queued...";
                existing.CanCancel = true;
                existing.CancelVisibility = Visibility.Visible;
                existing.DismissVisibility = Visibility.Collapsed;
                UpdateSummary();
                result = existing;
                return;
            }

            var item = new UpgradeProgressItemViewModel
            {
                PackageId = packageId,
                PackageName = packageName,
                State = UpgradeState.Queued,
                ProgressPercentage = 0,
                StatusMessage = "Queued...",
                CancellationTokenSource = cts ?? new CancellationTokenSource(),
                CanCancel = true,
                CancelVisibility = Visibility.Visible,
                DismissVisibility = Visibility.Collapsed
            };

            Items.Insert(0, item);
            UpdateSummary();
            result = item;
        });

        return result;
    }

    public void ReportProgress(UpgradeProgress progress)
    {
        RunOnUIThread(() =>
        {
            var item = Items.FirstOrDefault(i => string.Equals(i.PackageId, progress.PackageId, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.UpdateProgress(progress);
                UpdateSummary();

                if (progress.State == UpgradeState.Completed)
                {
                    ScheduleAutoClear(item);
                }
            }

            ProgressReported?.Invoke(progress);
        });
    }

    private void ScheduleAutoClear(UpgradeProgressItemViewModel item)
    {
        Task.Delay(4000).ContinueWith(_ =>
        {
            RunOnUIThread(() =>
            {
                if (item.State == UpgradeState.Completed && Items.Contains(item))
                {
                    Items.Remove(item);
                    UpdateSummary();
                }
            });
        });
    }

    [RelayCommand]
    public void TogglePanel()
    {
        IsPanelExpanded = !IsPanelExpanded;
        PanelDrawerVisibility = IsPanelExpanded ? Visibility.Visible : Visibility.Collapsed;
    }

    [RelayCommand]
    public void CancelPackage(UpgradeProgressItemViewModel? item)
    {
        if (item == null) return;

        RunOnUIThread(() =>
        {
            try
            {
                if (!item.CancellationTokenSource.IsCancellationRequested)
                {
                    item.CancellationTokenSource.Cancel();
                }
            }
            catch (ObjectDisposedException) { }

            item.State = UpgradeState.Cancelled;
            item.StatusMessage = "Cancelled by user";
            item.CanCancel = false;
            item.CanRetry = true;
            item.CancelVisibility = Visibility.Collapsed;
            item.DismissVisibility = Visibility.Visible;
            UpdateSummary();
            ProgressReported?.Invoke(new UpgradeProgress(item.PackageId, UpgradeState.Cancelled, 0, "Cancelled by user"));
        });
    }

    [RelayCommand]
    public void CancelAll()
    {
        RunOnUIThread(() =>
        {
            foreach (var item in Items.Where(i => i.CanCancel).ToList())
            {
                try
                {
                    if (!item.CancellationTokenSource.IsCancellationRequested)
                    {
                        item.CancellationTokenSource.Cancel();
                    }
                }
                catch (ObjectDisposedException) { }

                item.State = UpgradeState.Cancelled;
                item.StatusMessage = "Cancelled by user";
                item.CanCancel = false;
                item.CanRetry = true;
                item.CancelVisibility = Visibility.Collapsed;
                item.DismissVisibility = Visibility.Visible;
            }
            UpdateSummary();
        });
    }

    [RelayCommand]
    public void ClearCompleted()
    {
        RunOnUIThread(() =>
        {
            var finished = Items.Where(i => i.State == UpgradeState.Completed || i.State == UpgradeState.Cancelled).ToList();
            foreach (var item in finished)
            {
                Items.Remove(item);
            }
            UpdateSummary();
        });
    }

    [RelayCommand]
    public void DismissItem(UpgradeProgressItemViewModel? item)
    {
        if (item == null) return;
        RunOnUIThread(() =>
        {
            Items.Remove(item);
            UpdateSummary();
        });
    }

    private void UpdateSummary()
    {
        CompletedCount = Items.Count(i => i.State == UpgradeState.Completed);
        InProgressCount = Items.Count(i => i.State == UpgradeState.Queued || i.State == UpgradeState.Downloading || i.State == UpgradeState.Installing);
        FailedCount = Items.Count(i => i.State == UpgradeState.Failed);
        HasActiveJobs = InProgressCount > 0;
        HasItems = Items.Count > 0;

        QueueBarVisibility = HasItems ? Visibility.Visible : Visibility.Collapsed;
        ActiveJobsVisibility = HasActiveJobs ? Visibility.Visible : Visibility.Collapsed;
        IdleJobsVisibility = HasActiveJobs ? Visibility.Collapsed : Visibility.Visible;
        PanelDrawerVisibility = IsPanelExpanded ? Visibility.Visible : Visibility.Collapsed;

        int total = Items.Count;
        if (HasActiveJobs)
        {
            int finished = CompletedCount + FailedCount;
            double sum = Items.Sum(i => i.ProgressPercentage);
            OverallPercentage = total > 0 ? sum / total : 0;
            SummaryStatusText = $"Updating {finished + 1} of {total} · {Math.Round(OverallPercentage)}%";
        }
        else if (FailedCount > 0)
        {
            SummaryStatusText = $"{FailedCount} update(s) failed";
            OverallPercentage = 100;
        }
        else if (CompletedCount > 0)
        {
            SummaryStatusText = $"{CompletedCount} update(s) completed";
            OverallPercentage = 100;
        }
        else
        {
            SummaryStatusText = "No active updates";
            OverallPercentage = 0;
        }
    }
}
