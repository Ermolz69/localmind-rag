using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddEmbeddings(this IServiceCollection services)
    {
        services.AddScoped<IDocumentEmbeddingService, DocumentEmbeddingService>();
        services.AddSingleton<EmbeddingModelCatalog>();
        services.AddSingleton(provider => new EmbeddingModelStore(
            provider.GetRequiredService<IAppPathProvider>(),
            provider.GetRequiredService<IOptions<AiOptions>>(),
            provider.GetRequiredService<EmbeddingModelCatalog>(),
            new HttpClient()));
        services.AddSingleton<IEmbeddingGenerator, ProviderEmbeddingGenerator>();

        return services;
    }
}
