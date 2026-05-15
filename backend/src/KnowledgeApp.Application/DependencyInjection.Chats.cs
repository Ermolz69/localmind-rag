using KnowledgeApp.Application.Chats;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.Application;

public static partial class DependencyInjection
{
    private static IServiceCollection AddChatApplication(this IServiceCollection services)
    {
        services.AddScoped<ChatRequestValidator>();
        services.AddScoped<CreateChatHandler>();
        services.AddScoped<GetChatMessagesHandler>();
        services.AddScoped<GetChatsHandler>();
        services.AddScoped<GetConversationByIdHandler>();
        services.AddScoped<SendChatMessageHandler>();
        services.AddScoped<UpdateConversationHandler>();

        return services;
    }
}
