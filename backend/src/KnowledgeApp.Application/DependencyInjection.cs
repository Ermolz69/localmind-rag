using KnowledgeApp.Application.Buckets;
using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Documents;
using KnowledgeApp.Application.Ingestion;
using KnowledgeApp.Application.Notes;
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
        services.AddScoped<CreateBucketHandler>();
        services.AddScoped<CreateChatHandler>();
        services.AddScoped<CreateNoteHandler>();
        services.AddScoped<DeleteBucketHandler>();
        services.AddScoped<DeleteDocumentHandler>();
        services.AddScoped<DeleteNoteHandler>();
        services.AddScoped<GetBucketsHandler>();
        services.AddScoped<GetChatsHandler>();
        services.AddScoped<GetNotesHandler>();
        services.AddScoped<ProcessIngestionJobHandler>();
        services.AddScoped<ReindexDocumentHandler>();
        services.AddScoped<SendChatMessageHandler>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<SettingsValidator>();
        services.AddScoped<UpdateBucketHandler>();
        services.AddScoped<UpdateNoteHandler>();
        services.AddScoped<UploadDocumentCommandValidator>();
        services.AddScoped<UploadDocumentHandler>();

        return services;
    }
}
