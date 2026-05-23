using KnowledgeApp.Application.Notes;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Notes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notes", async (
                Guid? bucketId,
                string? query,
                string? cursor,
                int? limit,
                GetNotesHandler handler,
                CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetNotesQuery(bucketId, query, cursor, limit ?? 50), cancellationToken)))
            .WithName("ListNotes")
            .WithTags("Notes")
            .WithSummary("Lists notes.")
            .WithDescription("Returns a cursor-paged note list filtered by optional bucket and search text.")
            .Produces<CursorPage<NoteDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPost("/api/notes", async (
            CreateNoteRequest request,
            CreateNoteHandler handler,
            CancellationToken cancellationToken) =>
        {
            NoteDto created = await handler.HandleAsync(request, cancellationToken);
            return Results.Created($"/api/notes/{created.Id}", created);
        })
            .WithName("CreateNote")
            .WithTags("Notes")
            .WithSummary("Creates a note.")
            .WithDescription("Creates a local note, optionally scoped to a bucket.")
            .Produces<NoteDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapPut("/api/notes/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            UpdateNoteRequest request,
            UpdateNoteHandler handler,
            CancellationToken cancellationToken) =>
        {
            UpdateNoteResult result = await handler.HandleAsync(id, request, cancellationToken);
            if (!result.Found)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        })
            .WithName("UpdateNote")
            .WithTags("Notes")
            .WithSummary("Updates a note.")
            .WithDescription("Updates note title, body, and optional bucket assignment.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapDelete("/api/notes/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            DeleteNoteHandler handler,
            CancellationToken cancellationToken) =>
        {
            DeleteNoteResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        })
            .WithName("DeleteNote")
            .WithTags("Notes")
            .WithSummary("Deletes a note.")
            .WithDescription("Soft-deletes a local note.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
