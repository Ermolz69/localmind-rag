using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Documents;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/documents", async (
                Guid? bucketId,
                string? status,
                string? cursor,
                int? limit,
                GetDocumentsHandler handler,
                CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(
                new GetDocumentsQuery(bucketId, status, cursor, limit ?? 50),
                cancellationToken)));

        app.MapPost("/api/documents/upload", async (
            IFormFile file,
            Guid? bucketId,
            UploadDocumentHandler handler,
            CancellationToken cancellationToken) =>
        {
            await using Stream? stream = file.OpenReadStream();
            UploadDocumentResponse? response = await handler.HandleAsync(
                new UploadDocumentCommand(stream, file.FileName, file.ContentType, file.Length, bucketId),
                cancellationToken);

            return Results.Created($"/api/documents/{response.DocumentId}", response);
        }).DisableAntiforgery();

        app.MapGet("/api/documents/{id:guid}", async (
            Guid id,
            GetDocumentByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            DocumentDto? document = await handler.HandleAsync(new GetDocumentByIdQuery(id), cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        });

        app.MapDelete("/api/documents/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id,
            DeleteDocumentHandler handler,
            CancellationToken cancellationToken) =>
        {
            DeleteDocumentResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.NoContent();
        });

        app.MapPost("/api/documents/{id:guid}/reindex", async (
            Guid id,
            ReindexDocumentHandler handler,
            CancellationToken cancellationToken) =>
        {
            ReindexDocumentResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return Results.NotFound();
            }

            return Results.Accepted(
                $"/api/ingestion/jobs/{result.JobId}",
                new ReindexDocumentResponse(id, result.JobId!.Value, result.Status ?? "Queued"));
        });

        return app;
    }
}
