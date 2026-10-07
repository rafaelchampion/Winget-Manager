using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WingetManager.Application.UseCases;
using WingetManager.Domain.Entities;

namespace WingetManager.UI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly SearchAndInstallUseCase _searchAndInstallUseCase;
    private readonly UpgradeQueueViewModel _queueViewModel;
    private readonly ILogger<SearchViewModel> _logger;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PackageItemViewModel> _searchResults = [];

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _infoBarTitle = string.Empty;

    [ObservableProperty]
    private string _infoBarMessage = string.Empty;

    [ObservableProperty]
    private bool _isInfoBarOpen;

    public SearchViewModel(
        SearchAndInstallUseCase searchAndInstallUseCase,
        UpgradeQueueViewModel queueViewModel,
        ILogger<SearchViewModel> logger)
    {
        _searchAndInstallUseCase = searchAndInstallUseCase;
        _queueViewModel = queueViewModel;
        _logger = logger;
    }

    [RelayCommand]
    public async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || IsSearching) return;

        IsSearching = true;
        StatusMessage = $"Searching winget for '{SearchQuery}'...";
        IsInfoBarOpen = false;

        try
        {
            var results = await _searchAndInstallUseCase.SearchAsync(SearchQuery);
            SearchResults = new ObservableCollection<PackageItemViewModel>(
                results.Select(p => new PackageItemViewModel(p)));

            StatusMessage = SearchResults.Count > 0 
                ? $"Found {SearchResults.Count} packages" 
                : "No matching packages found.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during search");
            StatusMessage = "Search error.";
            InfoBarTitle = "Search Error";
            InfoBarMessage = ex.Message;
            IsInfoBarOpen = true;
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    public async Task InstallAsync(PackageItemViewModel? package)
    {
        if (package == null) return;

        StatusMessage = $"Installing {package.Name}...";
        using var cts = new CancellationTokenSource();
        _queueViewModel.EnqueuePackage(package.Id, package.Name, cts);

        var progress = new Progress<UpgradeProgress>(report =>
        {
            _queueViewModel.ReportProgress(report);
        });

        try
        {
            var result = await _searchAndInstallUseCase.InstallAsync(package.Id, package.AvailableVersion, progress, cts.Token);
            if (result.Success)
            {
                InfoBarTitle = "Installed";
                InfoBarMessage = $"{package.Name} installed successfully!";
            }
            else
            {
                InfoBarTitle = "Install Failed";
                InfoBarMessage = result.ErrorMessage ?? "Installation encountered an issue.";
            }
            IsInfoBarOpen = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install package {PackageId}", package.Id);
            InfoBarTitle = "Install Error";
            InfoBarMessage = ex.Message;
            IsInfoBarOpen = true;
        }
    }
}
