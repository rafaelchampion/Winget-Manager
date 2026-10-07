using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using WingetManager.Application.Services;
using WingetManager.Application.UseCases;
using WingetManager.Domain.Entities;

namespace WingetManager.UI.ViewModels;

public partial class AppsViewModel : ObservableObject
{
    private readonly IPackageCacheService _cacheService;
    private readonly ScanForUpgradesUseCase _scanUseCase;
    private readonly UpgradePackagesUseCase _upgradeUseCase;
    private readonly ManageExclusionsUseCase _exclusionsUseCase;
    private readonly ManageVersionPinsUseCase _pinsUseCase;
    private readonly UpgradeQueueViewModel _queueViewModel;
    private readonly ILogger<AppsViewModel> _logger;

    public UpgradeQueueViewModel QueueViewModel => _queueViewModel;

    private List<PackageItemViewModel> _allPackages = [];
    private DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private ObservableCollection<PackageItemViewModel> _filteredPackages = [];

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private string _selectedFilter = "All"; // "All", "Updates", "Protected"

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isUpgrading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _lastScannedText = string.Empty;

    [ObservableProperty]
    private int _totalAppsCount;

    [ObservableProperty]
    private int _updatesAvailableCount;

    [ObservableProperty]
    private int _protectedCount;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private bool _hasSelectedPackages;

    [ObservableProperty]
    private bool _hasUpdates;

    [ObservableProperty]
    private string _infoBarTitle = string.Empty;

    [ObservableProperty]
    private string _infoBarMessage = string.Empty;

    [ObservableProperty]
    private bool _isInfoBarOpen;

    public AppsViewModel(
        IPackageCacheService cacheService,
        ScanForUpgradesUseCase scanUseCase,
        UpgradePackagesUseCase upgradeUseCase,
        ManageExclusionsUseCase exclusionsUseCase,
        ManageVersionPinsUseCase pinsUseCase,
        UpgradeQueueViewModel queueViewModel,
        ILogger<AppsViewModel> logger)
    {
        _cacheService = cacheService;
        _scanUseCase = scanUseCase;
        _upgradeUseCase = upgradeUseCase;
        _exclusionsUseCase = exclusionsUseCase;
        _pinsUseCase = pinsUseCase;
        _queueViewModel = queueViewModel;
        _logger = logger;

        _queueViewModel.ProgressReported += OnUpgradeProgressReported;
    }

    public void InitializeDispatcher(DispatcherQueue dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
        _queueViewModel.InitializeDispatcher(dispatcherQueue);
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

    public async Task InitializeAsync()
    {
        // 1. Fast Stale-while-revalidate: load snapshot from local cache immediately
        await LoadCachedSnapshotAsync();

        // 2. Non-blocking background rescan for fresh updates and newly installed apps
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteScanAsync(forceRefresh: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background rescan encountered an issue");
            }
        });
    }

    private async Task LoadCachedSnapshotAsync()
    {
        try
        {
            var snapshot = await _cacheService.GetCachedSnapshotAsync();
            var exclusions = await _exclusionsUseCase.GetExclusionsAsync();
            var pins = await _pinsUseCase.GetPinsAsync();
            var pinnedSet = new HashSet<string>(pins.Select(p => p.PackageId), StringComparer.OrdinalIgnoreCase);
            var excludedSet = new HashSet<string>(exclusions, StringComparer.OrdinalIgnoreCase);

            var upgradesDict = snapshot.UpgradablePackages.ToDictionary(
                p => p.Id, 
                p => p.AvailableVersion, 
                StringComparer.OrdinalIgnoreCase);

            var viewModels = new List<PackageItemViewModel>();
            foreach (var pkg in snapshot.InstalledPackages)
            {
                var packageToUse = pkg;
                if (upgradesDict.TryGetValue(pkg.Id, out var availVer) && !string.IsNullOrWhiteSpace(availVer))
                {
                    packageToUse = pkg with { AvailableVersion = availVer };
                }

                var vm = new PackageItemViewModel(packageToUse)
                {
                    IsPinned = pinnedSet.Contains(pkg.Id),
                    IsExcluded = excludedSet.Contains(pkg.Id)
                };
                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(PackageItemViewModel.IsSelected))
                    {
                        UpdateSelectedCount();
                    }
                };
                viewModels.Add(vm);
            }

            RunOnUIThread(() =>
            {
                _allPackages = SortPackages(viewModels);
                UpdateCounts();
                ApplyFilter();
                UpdateLastScannedText(snapshot.UpgradesScannedAt ?? snapshot.InstalledPackagesScannedAt);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cached package snapshot");
        }
    }

    [RelayCommand]
    public async Task ScanAsync()
    {
        await ExecuteScanAsync(forceRefresh: true);
    }

    public async Task ExecuteScanAsync(bool forceRefresh = false)
    {
        if (IsScanning) return;

        RunOnUIThread(() =>
        {
            IsScanning = true;
            StatusMessage = "Checking for updates...";
            IsInfoBarOpen = false;
        });

        try
        {
            // Scan installed packages and upgrades
            var installedTask = _cacheService.GetInstalledPackagesAsync(forceRefresh);
            var upgradesTask = _scanUseCase.ExecuteAsync(forceRefresh);

            await Task.WhenAll(installedTask, upgradesTask);

            var installed = await installedTask;
            var upgrades = await upgradesTask;

            var exclusions = await _exclusionsUseCase.GetExclusionsAsync();
            var pins = await _pinsUseCase.GetPinsAsync();
            var pinnedSet = new HashSet<string>(pins.Select(p => p.PackageId), StringComparer.OrdinalIgnoreCase);
            var excludedSet = new HashSet<string>(exclusions, StringComparer.OrdinalIgnoreCase);

            var upgradesDict = upgrades.ToDictionary(
                p => p.Id, 
                p => p.AvailableVersion, 
                StringComparer.OrdinalIgnoreCase);

            var newVms = new List<PackageItemViewModel>();
            foreach (var pkg in installed)
            {
                var packageToUse = pkg;
                if (upgradesDict.TryGetValue(pkg.Id, out var availVer) && !string.IsNullOrWhiteSpace(availVer))
                {
                    packageToUse = pkg with { AvailableVersion = availVer };
                }

                // Preserve any existing selection state if package was already listed
                var existing = _allPackages.FirstOrDefault(p => string.Equals(p.Id, pkg.Id, StringComparison.OrdinalIgnoreCase));
                bool isSelected = existing?.IsSelected ?? false;

                var vm = new PackageItemViewModel(packageToUse, isSelected)
                {
                    IsPinned = pinnedSet.Contains(pkg.Id),
                    IsExcluded = excludedSet.Contains(pkg.Id)
                };

                vm.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(PackageItemViewModel.IsSelected))
                    {
                        UpdateSelectedCount();
                    }
                };

                newVms.Add(vm);
            }

            RunOnUIThread(() =>
            {
                _allPackages = SortPackages(newVms);
                UpdateCounts();
                ApplyFilter();
                UpdateLastScannedText(_cacheService.UpgradesScannedAt ?? DateTimeOffset.UtcNow);
                StatusMessage = UpdatesAvailableCount > 0 
                    ? $"{UpdatesAvailableCount} update(s) available" 
                    : "All apps are up to date";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan for updates");
            RunOnUIThread(() =>
            {
                StatusMessage = "Scan failed";
                InfoBarTitle = "Update Check Error";
                InfoBarMessage = ex.Message;
                IsInfoBarOpen = true;
            });
        }
        finally
        {
            RunOnUIThread(() =>
            {
                IsScanning = false;
            });
        }
    }

    private static List<PackageItemViewModel> SortPackages(List<PackageItemViewModel> list)
    {
        // Packages with updates first, then alphabetical
        return list
            .OrderByDescending(p => p.HasUpdate && !p.IsExcluded && !p.IsPinned)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void UpdateCounts()
    {
        TotalAppsCount = _allPackages.Count;
        UpdatesAvailableCount = _allPackages.Count(p => p.HasUpdate && !p.IsExcluded && !p.IsPinned);
        ProtectedCount = _allPackages.Count(p => p.IsPinned || p.IsExcluded);
        HasUpdates = UpdatesAvailableCount > 0;
    }

    private void UpdateLastScannedText(DateTimeOffset? timestamp)
    {
        if (!timestamp.HasValue)
        {
            LastScannedText = string.Empty;
            return;
        }

        var local = timestamp.Value.ToLocalTime();
        var elapsed = DateTimeOffset.Now - local;

        if (elapsed.TotalMinutes < 2)
        {
            LastScannedText = "Checked just now";
        }
        else if (elapsed.TotalHours < 1)
        {
            LastScannedText = $"Checked {Math.Max(1, (int)elapsed.TotalMinutes)}m ago";
        }
        else if (elapsed.TotalHours < 24)
        {
            LastScannedText = $"Checked {(int)elapsed.TotalHours}h ago";
        }
        else
        {
            LastScannedText = $"Checked {local:d MMM}";
        }
    }

    partial void OnSearchFilterChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    public void SetFilter(string filter)
    {
        if (SelectedFilter != filter)
        {
            SelectedFilter = filter;
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        IEnumerable<PackageItemViewModel> filtered = _allPackages;

        // Apply filter chip
        if (SelectedFilter == "Updates")
        {
            filtered = filtered.Where(p => p.HasUpdate && !p.IsExcluded && !p.IsPinned);
        }
        else if (SelectedFilter == "Protected")
        {
            filtered = filtered.Where(p => p.IsPinned || p.IsExcluded);
        }

        // Apply search query
        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            filtered = filtered.Where(p =>
                p.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                (p.Source != null && p.Source.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredPackages = new ObservableCollection<PackageItemViewModel>(filtered);
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = FilteredPackages.Count(p => p.IsSelected);
        HasSelectedPackages = SelectedCount > 0;
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var p in FilteredPackages)
        {
            p.IsSelected = true;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public void DeselectAll()
    {
        foreach (var p in FilteredPackages)
        {
            p.IsSelected = false;
        }
        UpdateSelectedCount();
    }

    [RelayCommand]
    public async Task UpgradeAllUpdatesAsync()
    {
        var updatable = _allPackages.Where(p => p.HasUpdate && !p.IsExcluded && !p.IsPinned).ToList();
        if (updatable.Count == 0 || IsUpgrading) return;

        await UpgradePackageListAsync(updatable);
    }

    [RelayCommand]
    public async Task UpgradeSelectedAsync()
    {
        var selected = FilteredPackages.Where(p => p.IsSelected).ToList();
        if (selected.Count == 0 || IsUpgrading) return;

        await UpgradePackageListAsync(selected);
    }

    [RelayCommand]
    public async Task UpgradeSingleAsync(PackageItemViewModel? package)
    {
        if (package == null || IsUpgrading) return;
        await UpgradePackageListAsync([package]);
    }

    private async Task UpgradePackageListAsync(IReadOnlyList<PackageItemViewModel> packagesToUpgrade)
    {
        if (packagesToUpgrade.Count == 0) return;

        RunOnUIThread(() =>
        {
            IsUpgrading = true;
            StatusMessage = $"Upgrading {packagesToUpgrade.Count} package(s)...";
        });

        var tasks = new List<UpgradePackageTask>();
        foreach (var pkg in packagesToUpgrade)
        {
            var cts = new CancellationTokenSource();
            _queueViewModel.EnqueuePackage(pkg.Id, pkg.Name, cts);
            tasks.Add(new UpgradePackageTask(pkg.Id, cts.Token));
        }

        var progress = new Progress<UpgradeProgress>(report =>
        {
            _queueViewModel.ReportProgress(report);
        });

        try
        {
            var results = await _upgradeUseCase.ExecuteAsync(tasks, progress);
            int successful = results.Count(r => r.Success);
            int failed = results.Count(r => !r.Success);

            RunOnUIThread(() =>
            {
                InfoBarTitle = "Upgrade Complete";
                InfoBarMessage = successful > 0 
                    ? $"{successful} app(s) upgraded successfully{(failed > 0 ? $", {failed} failed" : "")}."
                    : "No packages were upgraded.";
                IsInfoBarOpen = true;

                // For successfully upgraded packages, clear HasUpdate in memory
                var upgradedIds = results.Where(r => r.Success).Select(r => r.PackageId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var p in _allPackages.Where(p => upgradedIds.Contains(p.Id)))
                {
                    p.Item = p.Item with { AvailableVersion = null };
                    p.IsSelected = false;
                }

                _allPackages = SortPackages(_allPackages);
                UpdateCounts();
                ApplyFilter();
                StatusMessage = $"{successful} upgraded successfully";
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upgrade operation failed");
            RunOnUIThread(() =>
            {
                InfoBarTitle = "Upgrade Error";
                InfoBarMessage = ex.Message;
                IsInfoBarOpen = true;
            });
        }
        finally
        {
            RunOnUIThread(() =>
            {
                IsUpgrading = false;
            });
        }
    }

    [RelayCommand]
    public async Task TogglePinAsync(PackageItemViewModel? package)
    {
        if (package == null) return;

        try
        {
            if (package.IsPinned)
            {
                await _pinsUseCase.UnpinVersionAsync(package.Id);
                package.IsPinned = false;
            }
            else
            {
                await _pinsUseCase.PinVersionAsync(package.Id, package.InstalledVersion, "Pinned from Apps");
                package.IsPinned = true;
            }

            _allPackages = SortPackages(_allPackages);
            UpdateCounts();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle pin for {PackageId}", package.Id);
        }
    }

    [RelayCommand]
    public async Task ToggleExclusionAsync(PackageItemViewModel? package)
    {
        if (package == null) return;

        try
        {
            if (package.IsExcluded)
            {
                await _exclusionsUseCase.RemoveExclusionAsync(package.Id);
                package.IsExcluded = false;
            }
            else
            {
                await _exclusionsUseCase.AddExclusionAsync(package.Id);
                package.IsExcluded = true;
            }

            _allPackages = SortPackages(_allPackages);
            UpdateCounts();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle exclusion for {PackageId}", package.Id);
        }
    }

    private void OnUpgradeProgressReported(UpgradeProgress progress)
    {
        RunOnUIThread(() =>
        {
            var package = _allPackages.FirstOrDefault(p =>
                string.Equals(p.Id, progress.PackageId, StringComparison.OrdinalIgnoreCase));
            package?.UpdateUpgradeProgress(progress);
        });
    }
}
