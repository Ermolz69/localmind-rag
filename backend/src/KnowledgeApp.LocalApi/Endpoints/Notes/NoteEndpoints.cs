using KnowledgeApp.Application.Notes;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Notes;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/notes", async (
            Guid? bucketId,
            string? query,
            string? cursor,
            int? limit,
            GetNotesHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(
                new GetNotesQuery(bucketId, query, cursor, limit ?? 50),
                cancellationToken)).ToApiResult(context))
            .WithName("ListNotes")
            .WithTags("Notes")
            .WithSummary("Lists notes.")
            .WithDescription("Returns a cursor-paged note list filtered by optional bucket and search text.")
            .Produces<ApiResponse<CursorPage<NoteDto>>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/notes", async (
            CreateNoteRequest request,
            CreateNoteHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(request, cancellationToken))
                .ToCreatedApiResult(context, created => $"{ApiVersions.V1Prefix}/notes/{created.Id}");
        })
            .WithName("CreateNote")
            .WithTags("Notes")
            .WithSummary("Creates a note.")
            .WithDescription("Creates a local note, optionally scoped to a bucket.")
            .Produces<ApiResponse<NoteDto>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPut("/notes/{id:guid}", async (
            Guid id,
            UpdateNoteRequest request,
            UpdateNoteHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult(context);
        })
            .WithName("UpdateNote")
            .WithTags("Notes")
            .WithSummary("Updates a note.")
            .WithDescription("Updates note title, body, and optional bucket assignment.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapDelete("/notes/{id:guid}", async (
            Guid id,
            DeleteNoteHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, cancellationToken)).ToApiResult(context);
        })
            .WithName("DeleteNote")
            .WithTags("Notes")
            .WithSummary("Deletes a note.")
            .WithDescription("Soft-deletes a local note.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        return app;
    }
}
