using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services
            .AddBucketApplication()
            .AddDocumentApplication()
            .AddNoteApplication()
            .AddChatApplication()
            .AddSettingsApplication()
            .AddIngestionApplication();
    }
}
