using KnowledgeApp.Application.Chats;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chats", async (GetChatsHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        app.MapPost("/api/chats", async (
            Conversation conversation,
            CreateChatHandler handler,
            CancellationToken cancellationToken) =>
        {
            var created = await handler.HandleAsync(conversation, cancellationToken);
            return Results.Created($"/api/chats/{created.Id}", created);
        });

        app.MapPost("/api/chats/{id:guid}/messages", async (
            Guid id,
            ChatMessageRequest request,
            SendChatMessageHandler handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.HandleAsync(id, request, cancellationToken));
        });

        return app;
    }
}
