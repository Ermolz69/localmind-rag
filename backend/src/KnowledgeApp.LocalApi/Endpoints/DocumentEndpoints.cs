using KnowledgeApp.Application.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/documents", async (
                Guid? bucketId,
                GetDocumentsHandler handler,
                CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(new GetDocumentsQuery(bucketId), cancellationToken)));

        app.MapPost("/api/documents/upload", async (
            IFormFile file,
            Guid? bucketId,
            UploadDocumentHandler handler,
            CancellationToken cancellationToken) =>
        {
            await using var stream = file.OpenReadStream();
            var response = await handler.HandleAsync(
                new UploadDocumentCommand(stream, file.FileName, file.ContentType, file.Length, bucketId),
                cancellationToken);

            return Results.Created($"/api/documents/{response.DocumentId}", response);
        }).DisableAntiforgery();

        app.MapGet("/api/documents/{id:guid}", async (
            Guid id,
            GetDocumentByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            var document = await handler.HandleAsync(new GetDocumentByIdQuery(id), cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        });

        app.MapDelete("/api/documents/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var document = await db.Documents.FindAsync([id], cancellationToken);
            if (document is null)
            {
                return TypedResults.NotFound();
            }

            document.Status = DocumentStatus.Deleted;
            document.SyncStatus = SyncStatus.DeletedLocal;
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NoContent();
        });

        app.MapPost("/api/documents/{id:guid}/reindex", async (
            Guid id,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var document = await db.Documents.FindAsync([id], cancellationToken);
            if (document is null)
            {
                return Results.NotFound();
            }

            db.IngestionJobs.Add(new IngestionJob { DocumentId = id });
            await db.SaveChangesAsync(cancellationToken);
            return Results.Accepted();
        });

        return app;
    }
}
