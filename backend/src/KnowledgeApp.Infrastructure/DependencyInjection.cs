using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalRuntimeOptions>(configuration.GetSection("LocalRuntime"));
        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.Configure<OcrOptions>(configuration.GetSection("Ocr"));

        return services
            .AddRuntime()
            .AddPersistence()
            .AddSystemServices()
            .AddStorage()
            .AddIngestion()
            .AddEmbeddings()
            .AddSearch()
            .AddRag()
            .AddSync()
            .AddOcr();
    }
}
