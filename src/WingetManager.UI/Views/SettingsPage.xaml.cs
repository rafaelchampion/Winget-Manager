using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WingetManager.Domain.Entities;
using WingetManager.UI.ViewModels;

namespace WingetManager.UI.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = ((App)Microsoft.UI.Xaml.Application.Current).Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await ViewModel.LoadSettingsCommand.ExecuteAsync(null);
        };
    }

    private async void OnRemoveExclusionClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is string id)
        {
            await ViewModel.RemoveExclusionCommand.ExecuteAsync(id);
        }
    }

    private async void OnRemovePinClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is VersionPin pin)
        {
            await ViewModel.RemovePinCommand.ExecuteAsync(pin);
        }
    }
}
