using KnowledgeApp.Application.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddDocumentApplication(this IServiceCollection services)
    {
        services.AddScoped<DeleteDocumentHandler>();
        services.AddScoped<GetDocumentByIdHandler>();
        services.AddScoped<GetDocumentsHandler>();
        services.AddScoped<ReindexDocumentHandler>();
        services.AddScoped<UploadDocumentCommandValidator>();
        services.AddScoped<UploadDocumentHandler>();

        return services;
    }
}
