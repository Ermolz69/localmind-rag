using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddScoped<KnowledgeApp.Infrastructure.Persistence.Interceptors.SyncOutboxSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            IAppPathProvider? paths = provider.GetRequiredService<IAppPathProvider>();
            options.UseSqlite($"Data Source={paths.DatabasePath}");
            options.AddInterceptors(provider.GetRequiredService<KnowledgeApp.Infrastructure.Persistence.Interceptors.SyncOutboxSaveChangesInterceptor>());
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IDocumentRepository, Services.Persistence.DocumentRepository>();
        services.AddScoped<IConversationRepository, Services.Persistence.ConversationRepository>();
        services.AddScoped<IBucketRepository, Services.Persistence.BucketRepository>();
        services.AddScoped<INoteRepository, Services.Persistence.NoteRepository>();
        services.AddScoped<IEmbeddingStore, Services.Persistence.EmbeddingStore>();
        services.AddScoped<KnowledgeApp.Application.Abstractions.Rag.ISemanticCacheRepository, Services.Persistence.SemanticCacheRepository>();
        services.AddScoped<KnowledgeApp.Application.Common.Diagnostics.IOperationLogRepository, Diagnostics.OperationLogRepository>();

        return services;
    }
}
