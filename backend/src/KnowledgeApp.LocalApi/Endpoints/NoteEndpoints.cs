using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/notes", async (AppDbContext db, CancellationToken cancellationToken) =>
            Results.Ok(await db.Notes.ToArrayAsync(cancellationToken)));

        app.MapPost("/api/notes", async (Note note, AppDbContext db, CancellationToken cancellationToken) =>
        {
            db.Notes.Add(note);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/notes/{note.Id}", note);
        });

        app.MapPut("/api/notes/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            Note request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var note = await db.Notes.FindAsync([id], cancellationToken);
            if (note is null)
            {
                return TypedResults.NotFound();
            }

            note.Title = request.Title;
            note.Markdown = request.Markdown;
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NoContent();
        });

        app.MapDelete("/api/notes/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var note = await db.Notes.FindAsync([id], cancellationToken);
            if (note is null)
            {
                return TypedResults.NotFound();
            }

            db.Notes.Remove(note);
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NoContent();
        });

        return app;
    }
}
