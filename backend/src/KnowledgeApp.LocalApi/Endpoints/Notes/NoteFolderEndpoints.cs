using KnowledgeApp.Application.Notes;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Notes;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class NoteFolderEndpoints
{
    public static IEndpointRouteBuilder MapNoteFolderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/buckets/{bucketId:guid}/note-folders", async (
            Guid bucketId,
            GetNoteFoldersHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(bucketId, cancellationToken)).ToApiResult(context))
            .WithName("ListNoteFolders")
            .WithTags("Notes")
            .WithSummary("Lists note folders in a bucket.")
            .Produces<ApiResponse<IReadOnlyCollection<NoteFolderDto>>>();

        app.MapGet("/buckets/{bucketId:guid}/notes/tree", async (
            Guid bucketId,
            GetNotesTreeHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(bucketId, cancellationToken)).ToApiResult(context))
            .WithName("GetNotesTree")
            .WithTags("Notes")
            .WithSummary("Gets complete tree of folders and notes in a bucket.")
            .Produces<ApiResponse<NotesTreeResponse>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapPost("/buckets/{bucketId:guid}/note-folders", async (
            Guid bucketId,
            CreateNoteFolderRequest request,
            CreateNoteFolderHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(bucketId, request, cancellationToken))
                .ToCreatedApiResult(context, created => $"{ApiVersions.V1Prefix}/buckets/{bucketId}/note-folders"))
            .WithName("CreateNoteFolder")
            .WithTags("Notes")
            .WithSummary("Creates a note folder.")
            .Produces<ApiResponse<NoteFolderDto>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object?>>(StatusCodes.Status409Conflict);

        app.MapPut("/note-folders/{id:guid}", async (
            Guid id,
            UpdateNoteFolderRequest request,
            UpdateNoteFolderHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult(context))
            .WithName("UpdateNoteFolder")
            .WithTags("Notes")
            .WithSummary("Updates a note folder.")
            .Produces<ApiResponse<NoteFolderDto>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object?>>(StatusCodes.Status409Conflict);

        app.MapDelete("/note-folders/{id:guid}", async (
            Guid id,
            DeleteNoteFolderHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, cancellationToken)).ToApiResult(context))
            .WithName("DeleteNoteFolder")
            .WithTags("Notes")
            .WithSummary("Deletes a note folder.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);
        app.MapPost("/note-folders/{id:guid}/move", async (
            Guid id,
            MoveNoteFolderRequest request,
            MoveNoteFolderHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(id, request, cancellationToken)).ToApiResult(context))
            .WithName("MoveNoteFolder")
            .WithTags("Notes")
            .WithSummary("Moves a note folder.")
            .Produces<ApiResponse<NoteFolderDto>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<object?>>(StatusCodes.Status409Conflict);

        return app;
    }
}
