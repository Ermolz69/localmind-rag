using KnowledgeApp.Application.Chats;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Rag;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chats", async (
                string? cursor,
                int? limit,
                GetChatsHandler handler,
                CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetChatsQuery(cursor, limit ?? 50), cancellationToken)));

        app.MapGet("/api/chats/{id:guid}", async (
            Guid id,
            GetConversationByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            ConversationDto? conversation = await handler.HandleAsync(id, cancellationToken);
            return conversation is null ? Results.NotFound() : Results.Ok(conversation);
        });

        app.MapGet("/api/chats/{id:guid}/messages", async (
            Guid id,
            GetChatMessagesHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<ChatMessageDto>? messages = await handler.HandleAsync(id, cancellationToken);
            return messages is null ? Results.NotFound() : Results.Ok(messages);
        });

        app.MapPost("/api/chats", async (
            CreateConversationRequest request,
            CreateChatHandler handler,
            CancellationToken cancellationToken) =>
        {
            ConversationDto created = await handler.HandleAsync(request, cancellationToken);
            return Results.Created($"/api/chats/{created.Id}", created);
        });

        app.MapPut("/api/chats/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            UpdateConversationRequest request,
            UpdateConversationHandler handler,
            CancellationToken cancellationToken) =>
        {
            UpdateConversationResult result = await handler.HandleAsync(id, request, cancellationToken);
            if (!result.Found)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        });

        app.MapDelete("/api/chats/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            DeleteConversationHandler handler,
            CancellationToken cancellationToken) =>
        {
            DeleteConversationResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        });

        app.MapPost("/api/chats/{id:guid}/messages", async (
            Guid id,
            ChatMessageRequest request,
            SendChatMessageHandler handler,
            CancellationToken cancellationToken) =>
        {
            SendChatMessageResult result = await handler.HandleAsync(id, request, cancellationToken);
            return Results.Ok(result.Answer);
        });

        return app;
    }
}
