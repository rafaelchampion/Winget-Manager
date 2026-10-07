using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using WingetManager.Application.UseCases;
using WingetManager.Domain.Interfaces;
using WingetManager.Infrastructure.Elevation;
using WingetManager.Infrastructure.Logging;
using WingetManager.Infrastructure.Persistence;
using WingetManager.Infrastructure.Winget;
using WingetManager.UI.ViewModels;

namespace WingetManager.UI;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? _window;
    public IServiceProvider Services { get; }

    public App()
    {
        InitializeComponent();

        // Configure Serilog
        SerilogConfigurator.ConfigureLogging();

        // Build DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
        });

        // Repositories & Services
        services.AddSingleton<ISettingsRepository, JsonSettingsRepository>();
        services.AddSingleton<IPackageCacheStore, JsonPackageCacheStore>();
        services.AddSingleton<WingetComRepository>();
        services.AddSingleton<WingetCliRepository>();
        services.AddSingleton<IPackageRepository, HybridPackageRepository>();
        services.AddSingleton<WingetManager.Application.Services.IPackageCacheService, WingetManager.Application.Services.PackageCacheService>();
        services.AddSingleton<IElevationService, ProcessElevationService>();

        // Use cases
        services.AddTransient<ScanForUpgradesUseCase>();
        services.AddTransient<UpgradePackagesUseCase>();
        services.AddTransient<ManageExclusionsUseCase>();
        services.AddTransient<ManageVersionPinsUseCase>();
        services.AddTransient<SearchAndInstallUseCase>();

        // ViewModels (Singletons preserve state, search filters, and cache across navigation)
        services.AddSingleton<UpgradeQueueViewModel>();
        services.AddSingleton<AppsViewModel>();
        services.AddSingleton<SearchViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
