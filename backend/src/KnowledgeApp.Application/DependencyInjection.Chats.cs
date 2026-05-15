using KnowledgeApp.Application.Chats;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddChatApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateChatHandler>();
        services.AddScoped<GetChatsHandler>();
        services.AddScoped<SendChatMessageHandler>();

        return services;
    }
}
