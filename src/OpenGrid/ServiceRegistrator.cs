using Microsoft.Extensions.DependencyInjection;
using OpenGrid.Services;
using OpenGrid.Services.SessionPersist;
using OpenGrid.Services.Telemetry;
using OpenGrid.ViewModels;

namespace OpenGrid;

public static class ServiceRegistrator
{
    public static IServiceCollection AddOpenGridServices(this IServiceCollection services)
    {
        // Register services
        services.AddSingleton<ITelemetryClient, AcTelemetryClient>();
        services.AddSingleton<ITelemetryClient, DebugTelemetryClient>();
        services.AddSingleton<ITelemetryClient, DirtRallyTelemetryClient>();
        services.AddSingleton<ITelemetryService, TelemetryService>();
        services.AddSingleton<SharedMemoryBridgeLauncher>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<SessionRepository>();
        services.AddSingleton<IDashboardRepository, DashboardRepository>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<SteamWatcher>();
        services.AddSingleton<SteamGameManager>();

        // Register ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SessionsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SessionDetailsViewModel>();
        services.AddTransient<DevicesViewModel>();
        services.AddTransient<DashboardsViewModel>();
        services.AddTransient<LapSelectionViewModel>();

        return services;
    }
}
