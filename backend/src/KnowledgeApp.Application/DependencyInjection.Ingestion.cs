using KnowledgeApp.Application.Ingestion;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddIngestionApplication(this IServiceCollection services)
    {
        services.AddScoped<ProcessIngestionJobHandler>();

        return services;
    }
}
