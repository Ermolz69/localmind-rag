using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Runtime;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IAppPathProvider, AppPathProvider>();
        services.AddHostedService<LocalRuntimeInitializer>();
        services.AddSingleton<AiRuntimeManager>();
        services.AddSingleton<StubEmbeddingGenerator>();
        services.AddSingleton<StubChatModelClient>();
        services.AddSingleton<IAiRuntimeProvider>(provider => provider.GetRequiredService<AiRuntimeManager>());
        services.AddSingleton<IAiRuntimeProvider>(provider => provider.GetRequiredService<StubAiRuntimeProvider>());
        services.AddSingleton<StubAiRuntimeProvider>();
        services.AddSingleton<IAiRuntimeProviderRegistry, AiRuntimeProviderRegistry>();
        services.AddSingleton<IAiRuntimeManager>(provider => provider.GetRequiredService<AiRuntimeManager>());
        services.AddSingleton<IAiModelRegistry>(provider => provider.GetRequiredService<AiRuntimeManager>());
        services.AddSingleton<IAiRuntimeSetupService>(provider => new LlamaCppRuntimeSetupService(
            provider.GetRequiredService<IAppPathProvider>(),
            provider.GetRequiredService<IOptions<AiOptions>>(),
            provider.GetRequiredService<EmbeddingModelStore>(),
            new HttpClient(),
            provider.GetRequiredService<ILogger<LlamaCppRuntimeSetupService>>()));

        return services;
    }
}
