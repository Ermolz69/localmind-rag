using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddOcr(this IServiceCollection services)
    {
        services.AddSingleton<IOcrEngine, TesseractOcrEngine>();

        return services;
    }
}
