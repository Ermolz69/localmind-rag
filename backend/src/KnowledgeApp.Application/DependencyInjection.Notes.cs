using KnowledgeApp.Application.Notes;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddNoteApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateNoteHandler>();
        services.AddScoped<DeleteNoteHandler>();
        services.AddScoped<GetNotesHandler>();
        services.AddScoped<UpdateNoteHandler>();

        return services;
    }
}
