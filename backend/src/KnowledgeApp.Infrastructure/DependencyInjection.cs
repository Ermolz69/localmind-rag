using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Options.Validation;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<RuntimeOptions>, RuntimeOptionsValidator>();
        services.AddSingleton<IValidateOptions<EmbeddingOptions>, EmbeddingOptionsValidator>();
        services.AddSingleton<IValidateOptions<ChunkingOptions>, ChunkingOptionsValidator>();
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        services.AddSingleton<IValidateOptions<DocumentPreviewOptions>, DocumentPreviewOptionsValidator>();
        services.AddSingleton<IValidateOptions<DatabaseOptions>, DatabaseOptionsValidator>();
        services.AddSingleton<IValidateOptions<VectorIndexOptions>, VectorIndexOptionsValidator>();
        services.AddSingleton<IValidateOptions<RagOptions>, RagOptionsValidator>();

        services.AddOptions<RuntimeOptions>()
            .Bind(configuration.GetSection(RuntimeOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<EmbeddingOptions>()
            .Bind(configuration.GetSection(EmbeddingOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<ChunkingOptions>()
            .Bind(configuration.GetSection(ChunkingOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<DocumentPreviewOptions>()
            .Bind(configuration.GetSection(DocumentPreviewOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<VectorIndexOptions>()
            .Bind(configuration.GetSection(VectorIndexOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<RuntimeModeOptions>()
            .Bind(configuration.GetSection(RuntimeModeOptions.SectionName));

        services.AddOptions<RagOptions>()
            .Bind(configuration.GetSection(RagOptions.SectionName))
            .ValidateOnStart();

        services.Configure<IngestionWorkerOptions>(
            configuration.GetSection(IngestionWorkerOptions.SectionName));

        services.Configure<OcrOptions>(
            configuration.GetSection("Ocr"));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(KnowledgeApp.Application.AssemblyReference).Assembly));
        services.AddScoped<KnowledgeApp.Application.Abstractions.IDomainEventPublisher, KnowledgeApp.Infrastructure.Services.Events.MediatRDomainEventPublisher>();
        services.AddMemoryCache();
        services.AddSingleton<KnowledgeApp.Application.Settings.IAppSettingsCache, Settings.AppSettingsCache>();
        services.AddSingleton<KnowledgeApp.Application.Settings.ISettingsChangeSignal, Settings.SettingsChangeSignal>();

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
