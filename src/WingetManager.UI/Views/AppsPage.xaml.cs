using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using WingetManager.UI.ViewModels;

namespace WingetManager.UI.Views;

public sealed partial class AppsPage : Page
{
    public AppsViewModel ViewModel { get; }

    public AppsPage()
    {
        ViewModel = ((App)Microsoft.UI.Xaml.Application.Current).Services.GetRequiredService<AppsViewModel>();
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.InitializeDispatcher(DispatcherQueue);
        await ViewModel.InitializeAsync();
    }

    private void OnFilterAllClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.SetFilter("All");
    }

    private void OnFilterUpdatesClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.SetFilter("Updates");
    }

    private void OnFilterProtectedClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.SetFilter("Protected");
    }

    private void OnUpgradeItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is PackageItemViewModel package)
        {
            ViewModel.UpgradeSingleCommand.Execute(package);
        }
    }

    private void OnTogglePinClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is PackageItemViewModel package)
        {
            ViewModel.TogglePinCommand.Execute(package);
        }
    }

    private void OnToggleExclusionClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is PackageItemViewModel package)
        {
            ViewModel.ToggleExclusionCommand.Execute(package);
        }
    }

    private void OnCopyPackageIdClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is PackageItemViewModel package)
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(package.Id);
            Clipboard.SetContent(dataPackage);
        }
    }
}
