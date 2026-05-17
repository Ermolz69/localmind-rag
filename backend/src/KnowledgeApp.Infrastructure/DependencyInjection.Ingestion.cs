using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddIngestion(this IServiceCollection services)
    {
        services.AddScoped<IIngestionQueue, IngestionQueue>();
        services.AddScoped<IIngestionJobProcessor, IngestionJobProcessor>();
        services.AddSingleton<RawTextExtractor>();
        services.AddSingleton<HtmlTextExtractor>();
        services.AddSingleton<PdfTextExtractor>();
        services.AddSingleton<DocxTextExtractor>();
        services.AddSingleton<PptxTextExtractor>();
        services.AddSingleton<IDocumentTextExtractorFactory, DocumentTextExtractorFactory>();
        services.AddSingleton<IDocumentChunker, SimpleDocumentChunker>();

        return services;
    }
}
