using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddSync(this IServiceCollection services)
    {
        services.AddScoped<SyncService>();
        services.AddScoped<ISyncService>(provider => provider.GetRequiredService<SyncService>());
        services.AddScoped<ISyncQueue>(provider => provider.GetRequiredService<SyncService>());
        services.AddScoped<ISyncClient>(provider => provider.GetRequiredService<SyncService>());

        return services;
    }
}
