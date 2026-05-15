using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Infrastructure.Services;
using KnowledgeApp.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddSystemServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ILocalDiagnosticsService, LocalDiagnosticsService>();
        services.AddSingleton<INetworkStatusService, NetworkStatusService>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAppLockService, AppLockService>();
        services.AddSingleton<ISettingsDefaultsProvider, SettingsDefaultsProvider>();

        return services;
    }
}
