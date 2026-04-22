using Microsoft.Extensions.DependencyInjection;
using SimLab.Services;
using SimLab.ViewModels;

namespace SimLab;

public static class ServiceRegistrator
{
    public static IServiceCollection AddSimLabServices(this IServiceCollection services)
    {
        // Register services
        services.AddSingleton<ISharedMemoryReader, AccSharedMemoryReader>();
        services.AddSingleton<SharedFileReader>();
        services.AddSingleton<SharedMemoryBridgeLauncher>();
        services.AddSingleton<INavigationService, NavigationService>();

        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SessionsViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }
}
