using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WingetManager.UI.ViewModels;

namespace WingetManager.UI.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        ViewModel = ((App)Microsoft.UI.Xaml.Application.Current).Services.GetRequiredService<SearchViewModel>();
        InitializeComponent();
    }

    private async void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await ViewModel.SearchCommand.ExecuteAsync(null);
        }
    }

    private async void OnInstallClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is PackageItemViewModel item)
        {
            await ViewModel.InstallCommand.ExecuteAsync(item);
        }
    }
}
