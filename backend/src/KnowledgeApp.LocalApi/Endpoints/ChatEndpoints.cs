using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chats", async (AppDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.Conversations.ToArrayAsync(cancellationToken)));

        app.MapPost("/api/chats", async (
            Conversation conversation,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            db.Conversations.Add(conversation);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/chats/{conversation.Id}", conversation);
        });

        app.MapPost("/api/chats/{id:guid}/messages", async (
            Guid id,
            ChatMessageRequest request,
            IRagAnswerGenerator rag,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            db.ChatMessages.Add(new ChatMessage { ConversationId = id, Role = ChatRole.User, Content = request.Content });
            var answer = await rag.AnswerAsync(id, request.Content, cancellationToken);
            db.ChatMessages.Add(new ChatMessage { ConversationId = id, Role = ChatRole.Assistant, Content = answer.Answer });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(answer);
        });

        return app;
    }
}
