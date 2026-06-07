using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddIngestionApplication(this IServiceCollection services)
    {
        services.AddScoped<ProcessIngestionJobHandler>();
        services.AddScoped<ListIngestionJobsHandler>();
        services.AddScoped<GetIngestionJobHandler>();
        services.AddScoped<RetryIngestionJobHandler>();
        services.AddScoped<CancelIngestionJobHandler>();
        services.AddScoped<KnowledgeApp.Application.Ingestion.WatchedFolders.Commands.CleanupWatchedFoldersHandler>();

        services.AddSingleton<IIncrementalChunkPlanner, IncrementalChunkPlanner>();

        return services;
    }
}
