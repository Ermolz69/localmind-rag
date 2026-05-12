using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Runtime;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalRuntimeOptions>(configuration.GetSection("LocalRuntime"));
        services.Configure<AiOptions>(configuration.GetSection("Ai"));

        services.AddSingleton<IAppPathProvider, AppPathProvider>();
        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var paths = provider.GetRequiredService<IAppPathProvider>();
            options.UseSqlite($"Data Source={paths.DatabasePath}");
        });
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddHostedService<LocalRuntimeInitializer>();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IIngestionQueue, IngestionQueue>();
        services.AddScoped<IIngestionJobProcessor, IngestionJobProcessor>();
        services.AddSingleton<RawTextExtractor>();
        services.AddSingleton<HtmlTextExtractor>();
        services.AddSingleton<IDocumentTextExtractorFactory, DocumentTextExtractorFactory>();
        services.AddSingleton<IDocumentChunker, SimpleDocumentChunker>();
        services.AddSingleton<IEmbeddingGenerator, StubEmbeddingGenerator>();
        services.AddSingleton<IChatModelClient, StubChatModelClient>();
        services.AddScoped<ExactVectorSearchService>();
        services.AddScoped<IVectorSearchService>(provider => provider.GetRequiredService<ExactVectorSearchService>());
        services.AddScoped<IVectorIndex>(provider => provider.GetRequiredService<ExactVectorSearchService>());
        services.AddScoped<IRagContextBuilder, RagContextBuilder>();
        services.AddScoped<IRagAnswerGenerator, RagAnswerGenerator>();
        services.AddSingleton<AiRuntimeManager>();
        services.AddSingleton<IAiRuntimeManager>(provider => provider.GetRequiredService<AiRuntimeManager>());
        services.AddSingleton<IAiModelRegistry>(provider => provider.GetRequiredService<AiRuntimeManager>());
        services.AddScoped<SyncService>();
        services.AddScoped<ISyncService>(provider => provider.GetRequiredService<SyncService>());
        services.AddScoped<ISyncQueue>(provider => provider.GetRequiredService<SyncService>());
        services.AddScoped<ISyncClient>(provider => provider.GetRequiredService<SyncService>());
        services.AddSingleton<INetworkStatusService, NetworkStatusService>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAppLockService, AppLockService>();

        return services;
    }
}
