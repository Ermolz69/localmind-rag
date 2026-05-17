using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Runtime;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IAppPathProvider, AppPathProvider>();
        services.AddHostedService<LocalRuntimeInitializer>();
        services.AddSingleton<AiRuntimeManager>();
        services.AddSingleton<IAiRuntimeManager>(provider => provider.GetRequiredService<AiRuntimeManager>());
        services.AddSingleton<IAiModelRegistry>(provider => provider.GetRequiredService<AiRuntimeManager>());

        return services;
    }
}
