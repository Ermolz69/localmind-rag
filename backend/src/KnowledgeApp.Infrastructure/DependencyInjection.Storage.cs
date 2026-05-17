using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddStorage(this IServiceCollection services)
    {
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        return services;
    }
}
