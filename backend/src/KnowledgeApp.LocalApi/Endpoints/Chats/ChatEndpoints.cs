using KnowledgeApp.Application.Chats;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Rag;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chats", async (
                string? cursor,
                int? limit,
                GetChatsHandler handler,
                HttpContext context,
                CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new GetChatsQuery(cursor, limit ?? 50), cancellationToken)).ToApiResult(context))
            .WithName("ListChats")
            .WithTags("Chats")
            .WithSummary("Lists conversations.")
            .WithDescription("Returns a cursor-paged list of local chat conversations.")
            .Produces<ApiResponse<CursorPage<ConversationDto>>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapGet("/api/chats/{id:guid}", async (
            Guid id,
            GetConversationByIdHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, cancellationToken)).ToApiResult(context);
        })
            .WithName("GetChat")
            .WithTags("Chats")
            .WithSummary("Gets a conversation.")
            .WithDescription("Returns a conversation by local identifier.")
            .Produces<ApiResponse<ConversationDto>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapGet("/api/chats/{id:guid}/messages", async (
            Guid id,
            GetChatMessagesHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, cancellationToken)).ToApiResult(context);
        })
            .WithName("ListChatMessages")
            .WithTags("Chats")
            .WithSummary("Lists conversation messages.")
            .WithDescription("Returns messages for an existing conversation.")
            .Produces<ApiResponse<IReadOnlyList<ChatMessageDto>>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapPost("/api/chats", async (
            CreateConversationRequest request,
            CreateChatHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedApiResult(context, created => $"/api/chats/{created.Id}");
        })
            .WithName("CreateChat")
            .WithTags("Chats")
            .WithSummary("Creates a conversation.")
            .WithDescription("Creates a local RAG chat conversation.")
            .Produces<ApiResponse<ConversationDto>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPut("/api/chats/{id:guid}", async (
            Guid id,
            UpdateConversationRequest request,
            UpdateConversationHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult(context);
        })
            .WithName("UpdateChat")
            .WithTags("Chats")
            .WithSummary("Updates a conversation.")
            .WithDescription("Updates the title of an existing conversation.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapDelete("/api/chats/{id:guid}", async (
            Guid id,
            DeleteConversationHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, cancellationToken)).ToApiResult(context);
        })
            .WithName("DeleteChat")
            .WithTags("Chats")
            .WithSummary("Deletes a conversation.")
            .WithDescription("Soft-deletes a conversation and hides its messages.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapPost("/api/chats/{id:guid}/messages", async (
            Guid id,
            ChatMessageRequest request,
            SendChatMessageHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult(context);
        })
            .WithName("SendChatMessage")
            .WithTags("Chats")
            .WithSummary("Sends a chat message.")
            .WithDescription("Adds a user message, builds RAG context, and returns an answer with sources.")
            .Produces<ApiResponse<RagAnswerDto>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        return app;
    }
}
