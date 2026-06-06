using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddSettingsApplication(this IServiceCollection services)
    {
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<SettingsValidator>();
        services.AddSingleton<IWatchedFolderPathValidator, WatchedFolderPathValidator>();
        services.AddSingleton<IWatchedFolderStatusStore, WatchedFolderStatusStore>();

        return services;
    }
}
