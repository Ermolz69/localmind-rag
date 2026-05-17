using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Devices;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddCommonApplication(this IServiceCollection services)
    {
        services.AddScoped<ILocalDeviceResolver, LocalDeviceResolver>();

        return services;
    }
}
