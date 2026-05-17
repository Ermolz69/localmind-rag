using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Infrastructure;

public static partial class DependencyInjection
{
    private static IServiceCollection AddRag(this IServiceCollection services)
    {
        services.AddSingleton<IChatModelClient, StubChatModelClient>();
        services.AddScoped<IRagContextBuilder, RagContextBuilder>();
        services.AddScoped<IRagAnswerGenerator, RagAnswerGenerator>();

        return services;
    }
}
