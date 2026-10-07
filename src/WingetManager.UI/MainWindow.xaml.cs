using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WingetManager.UI.ViewModels;
using WingetManager.UI.Views;

namespace WingetManager.UI;

public sealed partial class MainWindow : Window
{
    public UpgradeQueueViewModel QueueViewModel { get; }

    public MainWindow()
    {
        QueueViewModel = ((App)Microsoft.UI.Xaml.Application.Current).Services.GetRequiredService<UpgradeQueueViewModel>();
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        QueueViewModel.InitializeDispatcher(DispatcherQueue);

        // Default to AppsPage
        NavFrame.Navigate(typeof(AppsPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            Type? pageType = tag switch
            {
                "apps" => typeof(AppsPage),
                "discover" => typeof(SearchPage),
                _ => null
            };

            if (pageType != null && NavFrame.CurrentSourcePageType != pageType)
            {
                NavFrame.Navigate(pageType);
            }
        }
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack)
        {
            NavFrame.GoBack();
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void OnCancelQueueItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is UpgradeProgressItemViewModel item)
        {
            QueueViewModel.CancelPackageCommand.Execute(item);
        }
    }

    private void OnDismissQueueItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is UpgradeProgressItemViewModel item)
        {
            QueueViewModel.DismissItemCommand.Execute(item);
        }
    }
}
