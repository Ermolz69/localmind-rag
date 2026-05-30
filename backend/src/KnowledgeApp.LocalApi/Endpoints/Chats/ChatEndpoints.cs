using KnowledgeApp.Application.Chats;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Chats;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Rag;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/chats", async (
            string? cursor,
            int? limit,
            GetChatsHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(
                new GetChatsQuery(cursor, limit ?? 50),
                cancellationToken)).ToApiResult(context))
            .WithName("ListChats")
            .WithTags("Chats")
            .WithSummary("Lists conversations.")
            .WithDescription("Returns a cursor-paged list of local chat conversations.")
            .Produces<ApiResponse<CursorPage<ConversationDto>>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapGet("/chats/{id:guid}", async (
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

        app.MapGet("/chats/{id:guid}/messages", async (
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

        app.MapPost("/chats", async (
            CreateConversationRequest request,
            CreateChatHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedApiResult(context, created => $"{ApiVersions.V1Prefix}/chats/{created.Id}");
        })
            .WithName("CreateChat")
            .WithTags("Chats")
            .WithSummary("Creates a conversation.")
            .WithDescription("Creates a local RAG chat conversation.")
            .Produces<ApiResponse<ConversationDto>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPut("/chats/{id:guid}", async (
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

        app.MapDelete("/chats/{id:guid}", async (
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

        app.MapPost("/chats/{id:guid}/messages", async (
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

        app.MapPost("/chats/{id:guid}/messages/stream", async (
            Guid id,
            ChatMessageRequest request,
            SendChatStreamMessageHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            // Validate request and conversation presence before setting stream response headers.
            // This ensures early failures (e.g. 400 Bad Request, 404 Not Found) are handled
            // consistently by the global exception handler as standard JSON ApiResponse envelopes.
            await handler.ValidateAndPrepareAsync(id, request, cancellationToken);

            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            try
            {
                await foreach (RagAnswerChunkDto chunk in handler.HandleStreamAsync(id, request, cancellationToken))
                {
                    await context.Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(chunk, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })}\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Format the mid-stream error consistently with the backend error policy
                ApiResponse<object?> apiResponse = MapExceptionToApiResponse(ex, context);
                string json = System.Text.Json.JsonSerializer.Serialize(apiResponse, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                await context.Response.WriteAsync($"data: {json}\n\n", CancellationToken.None);
                await context.Response.Body.FlushAsync(CancellationToken.None);
            }
        })
            .WithName("StreamChatMessage")
            .WithTags("Chats")
            .WithSummary("Streams a chat message.")
            .WithDescription("Adds a user message, builds RAG context, and streams the answer with sources using Server-Sent Events.");

        return app;
    }

    private static ApiResponse<object?> MapExceptionToApiResponse(Exception exception, HttpContext context)
    {
        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();

        return exception switch
        {
            ValidationAppException valEx => ApiResponse.Failure(
                valEx.Code,
                valEx.Message,
                context.TraceIdentifier,
                valEx.Errors.SelectMany(error => error.Value.Select(msg => new ApiErrorDetail(error.Key, msg))).ToArray()),

            AppException appEx => ApiResponse.Failure(appEx.Code, appEx.Message, context.TraceIdentifier),

            ArgumentException argEx => ApiResponse.Failure(ErrorCodes.RequestInvalid, argEx.Message, context.TraceIdentifier),

            _ => ApiResponse.Failure(
                ErrorCodes.Unexpected,
                environment.IsDevelopment() ? ErrorMessages.UnexpectedDevelopment : ErrorMessages.UnexpectedProduction,
                context.TraceIdentifier)
        };
    }
}
