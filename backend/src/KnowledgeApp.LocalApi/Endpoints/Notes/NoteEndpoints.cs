using KnowledgeApp.Application.Notes;
using KnowledgeApp.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notes", async (GetNotesHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        app.MapPost("/api/notes", async (Note note, CreateNoteHandler handler, CancellationToken cancellationToken) =>
        {
            var created = await handler.HandleAsync(note, cancellationToken);
            return Results.Created($"/api/notes/{created.Id}", created);
        });

        app.MapPut("/api/notes/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            Note request,
            UpdateNoteHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!await handler.HandleAsync(id, request, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        });

        app.MapDelete("/api/notes/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            DeleteNoteHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!await handler.HandleAsync(id, cancellationToken))
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        });

        return app;
    }
}
