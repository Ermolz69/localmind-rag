using KnowledgeApp.Application.Notes;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddNoteApplication(this IServiceCollection services)
    {
        services.AddScoped<NoteRequestValidator>();
        services.AddScoped<CreateNoteHandler>();
        services.AddScoped<CreateNoteFolderHandler>();
        services.AddScoped<DeleteNoteHandler>();
        services.AddScoped<DeleteNoteFolderHandler>();
        services.AddScoped<GetNotesHandler>();
        services.AddScoped<GetNoteFoldersHandler>();
        services.AddScoped<GetNotesTreeHandler>();
        services.AddScoped<INoteFolderService, NoteFolderService>();
        services.AddScoped<MoveNoteFolderHandler>();
        services.AddScoped<MoveNoteHandler>();
        services.AddScoped<UpdateNoteHandler>();
        services.AddScoped<UpdateNoteFolderHandler>();

        return services;
    }
}
