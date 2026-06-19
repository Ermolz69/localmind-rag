using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddRag(this IServiceCollection services)
    {
        services.AddSingleton<IChatModelClient, ProviderChatModelClient>();
        services.AddScoped<IChatTitleGenerator, ChatTitleGenerator>();
        services.AddScoped<IRagContextBuilder, RagContextBuilder>();
        services.AddScoped<RagAnswerGenerator>();
        services.AddScoped<IRagAnswerGenerator>(provider =>
            new CachedRagAnswerGenerator(
                provider.GetRequiredService<RagAnswerGenerator>(),
                provider.GetRequiredService<KnowledgeApp.Application.Abstractions.Rag.ISemanticCacheRepository>(),
                provider.GetRequiredService<IEmbeddingGenerator>(),
                provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<KnowledgeApp.Infrastructure.Options.RagOptions>>(),
                provider.GetService<IAppDiagnosticLogger>()));

        return services;
    }
}
