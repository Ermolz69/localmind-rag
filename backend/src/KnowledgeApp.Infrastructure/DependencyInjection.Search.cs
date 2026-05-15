using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddSearch(this IServiceCollection services)
    {
        services.AddScoped<ExactVectorSearchService>();
        services.AddScoped<IVectorSearchService>(provider => provider.GetRequiredService<ExactVectorSearchService>());
        services.AddScoped<IVectorIndex>(provider => provider.GetRequiredService<ExactVectorSearchService>());

        return services;
    }
}
