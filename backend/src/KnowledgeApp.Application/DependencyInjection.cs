using KnowledgeApp.Application.Documents;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetDocumentByIdHandler>();
        services.AddScoped<GetDocumentsHandler>();
        services.AddScoped<UploadDocumentHandler>();

        return services;
    }
}
