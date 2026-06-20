using KnowledgeApp.Application.Companion;
using KnowledgeApp.Application.Companion.Files;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddCompanionApplication(this IServiceCollection services)
    {
        // Singleton: holds the in-memory pairing session and trusted-device list.
        services.AddSingleton<ICompanionPairingService, CompanionPairingService>();

        // Scoped: browses allowed folders and reuses the scoped upload handler.
        services.AddScoped<ICompanionFileService, CompanionFileService>();

        return services;
    }
}
