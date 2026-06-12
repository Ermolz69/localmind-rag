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
        services.AddSingleton<KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager>();

        services.AddHostedService<LocalRuntimeInitializer>();

        services.AddSingleton<AiRuntimeManager>();
        services.AddSingleton<ChatModelCatalog>();
        services.AddSingleton(provider =>
            new ChatModelStore(
                provider.GetRequiredService<IAppPathProvider>(),
                provider.GetRequiredService<IOptions<EmbeddingOptions>>(),
                provider.GetRequiredService<ChatModelCatalog>(),
                new HttpClient()));
        services.AddSingleton<StubEmbeddingGenerator>();
        services.AddSingleton<StubChatModelClient>();
        services.AddSingleton<StubAiRuntimeProvider>();

        services.AddSingleton<IAiRuntimeProvider>(provider =>
            provider.GetRequiredService<AiRuntimeManager>());

        services.AddSingleton<IAiRuntimeProvider>(provider =>
            provider.GetRequiredService<StubAiRuntimeProvider>());

        services.AddSingleton<IAiRuntimeProviderRegistry, AiRuntimeProviderRegistry>();

        services.AddSingleton<IAiRuntimeManager>(provider =>
            provider.GetRequiredService<AiRuntimeManager>());

        services.AddSingleton<IAiModelRegistry>(provider =>
            provider.GetRequiredService<AiRuntimeManager>());

        services.AddSingleton<IAiRuntimeSetupCoordinator, AiRuntimeSetupCoordinator>();

        services.AddSingleton<IAiRuntimeSetupService>(provider =>
            new LlamaCppRuntimeSetupService(
                provider.GetRequiredService<IAppPathProvider>(),
                provider.GetRequiredService<IOptions<RuntimeOptions>>(),
                provider.GetRequiredService<EmbeddingModelStore>(),
                provider.GetRequiredService<ChatModelStore>(),
                new HttpClient(),
                provider.GetRequiredService<ILogger<LlamaCppRuntimeSetupService>>()));

        return services;
    }
}
