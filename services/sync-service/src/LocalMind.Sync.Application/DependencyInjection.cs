namespace LocalMind.Sync.Application;

using LocalMind.Sync.Application.Conflicts;
using LocalMind.Sync.Application.Devices;
using LocalMind.Sync.Application.Sessions;
using LocalMind.Sync.Application.Sync;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddSyncApplication(this IServiceCollection services)
    {
        services.AddScoped<DeviceService>();
        services.AddScoped<SyncSessionService>();
        services.AddScoped<SyncService>();
        services.AddScoped<ConflictService>();
        services.AddSingleton<ManifestDiffCalculator>();
        return services;
    }
}
