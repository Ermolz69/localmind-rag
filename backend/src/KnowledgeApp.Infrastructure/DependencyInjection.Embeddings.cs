using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddEmbeddings(this IServiceCollection services)
    {
        services.AddScoped<IDocumentEmbeddingService, DocumentEmbeddingService>();
        services.AddSingleton<IEmbeddingGenerator, StubEmbeddingGenerator>();

        return services;
    }
}
