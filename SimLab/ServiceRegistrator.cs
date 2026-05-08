using Microsoft.Extensions.DependencyInjection;
using SimLab.Services;
using SimLab.Services;
using SimLab.Services.SharedMemory;
using SimLab.ViewModels;

namespace SimLab;

public static class ServiceRegistrator
{
    public static IServiceCollection AddSimLabServices(this IServiceCollection services)
    {
        // Register services
        services.AddSingleton<ITelemetryClient, AcTelemetryClient>();
        services.AddSingleton<ITelemetryClient, DebugTelemetryClient>();
        services.AddSingleton<ITelemetryService, TelemetryService>();
        services.AddSingleton<SharedMemoryBridgeLauncher>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<SessionRepository>();

        // Register ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SessionsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SessionDetailsViewModel>();
        services.AddTransient<DevicesViewModel>();
        services.AddTransient<DashboardsViewModel>();
        services.AddTransient<DashboardViewModel>();

        return services;
    }
}
