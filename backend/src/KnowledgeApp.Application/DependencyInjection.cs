using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Application.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetDocumentByIdHandler>();
        services.AddScoped<GetDocumentsHandler>();
        services.AddScoped<IBucketResolver, BucketResolver>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<SettingsValidator>();
        services.AddScoped<UploadDocumentCommandValidator>();
        services.AddScoped<UploadDocumentHandler>();

        return services;
    }
}
