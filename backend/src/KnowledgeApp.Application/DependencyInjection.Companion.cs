using KnowledgeApp.Application.Companion;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddCompanionApplication(this IServiceCollection services)
    {
        // Singleton: holds the in-memory pairing session and trusted-device list.
        services.AddSingleton<ICompanionPairingService, CompanionPairingService>();

        return services;
    }
}
