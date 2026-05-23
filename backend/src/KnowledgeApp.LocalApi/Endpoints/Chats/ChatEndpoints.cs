using KnowledgeApp.Application.Chats;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Common;
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
            Results.Ok(await handler.HandleAsync(new GetChatsQuery(cursor, limit ?? 50), cancellationToken)))
            .WithName("ListChats")
            .WithTags("Chats")
            .WithSummary("Lists conversations.")
            .WithDescription("Returns a cursor-paged list of local chat conversations.")
            .Produces<CursorPage<ConversationDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGet("/api/chats/{id:guid}", async (
            Guid id,
            GetConversationByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            ConversationDto? conversation = await handler.HandleAsync(id, cancellationToken);
            return conversation is null ? Results.NotFound() : Results.Ok(conversation);
        })
            .WithName("GetChat")
            .WithTags("Chats")
            .WithSummary("Gets a conversation.")
            .WithDescription("Returns a conversation by local identifier.")
            .Produces<ConversationDto>()
            .Produces(StatusCodes.Status404NotFound);

        app.MapGet("/api/chats/{id:guid}/messages", async (
            Guid id,
            GetChatMessagesHandler handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<ChatMessageDto>? messages = await handler.HandleAsync(id, cancellationToken);
            return messages is null ? Results.NotFound() : Results.Ok(messages);
        })
            .WithName("ListChatMessages")
            .WithTags("Chats")
            .WithSummary("Lists conversation messages.")
            .WithDescription("Returns messages for an existing conversation.")
            .Produces<IReadOnlyList<ChatMessageDto>>()
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/chats", async (
            CreateConversationRequest request,
            CreateChatHandler handler,
            CancellationToken cancellationToken) =>
        {
            ConversationDto created = await handler.HandleAsync(request, cancellationToken);
            return Results.Created($"/api/chats/{created.Id}", created);
        })
            .WithName("CreateChat")
            .WithTags("Chats")
            .WithSummary("Creates a conversation.")
            .WithDescription("Creates a local RAG chat conversation.")
            .Produces<ConversationDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

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
        })
            .WithName("UpdateChat")
            .WithTags("Chats")
            .WithSummary("Updates a conversation.")
            .WithDescription("Updates the title of an existing conversation.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

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
        })
            .WithName("DeleteChat")
            .WithTags("Chats")
            .WithSummary("Deletes a conversation.")
            .WithDescription("Soft-deletes a conversation and hides its messages.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        app.MapPost("/api/chats/{id:guid}/messages", async (
            Guid id,
            ChatMessageRequest request,
            SendChatMessageHandler handler,
            CancellationToken cancellationToken) =>
        {
            SendChatMessageResult result = await handler.HandleAsync(id, request, cancellationToken);
            return Results.Ok(result.Answer);
        })
            .WithName("SendChatMessage")
            .WithTags("Chats")
            .WithSummary("Sends a chat message.")
            .WithDescription("Adds a user message, builds RAG context, and returns an answer with sources.")
            .Produces<RagAnswerDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
