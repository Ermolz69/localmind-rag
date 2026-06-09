using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddSearchApplication(this IServiceCollection services)
    {
        services.AddScoped<SemanticSearchHandler>();
        services.AddScoped<SemanticSearchRequestValidator>();
        services.AddScoped<IHybridRetrievalService, HybridRetrievalService>();

        services.AddScoped<ContentSearchHandler>();
        services.AddScoped<ContentSearchRequestValidator>();

        return services;
    }
}
