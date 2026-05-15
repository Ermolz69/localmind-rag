using KnowledgeApp.Application.Documents;
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
            DeleteDocumentHandler handler,
            CancellationToken cancellationToken) =>
        {
            if (!await handler.HandleAsync(id, cancellationToken))
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
            if (!await handler.HandleAsync(id, cancellationToken))
            {
                return Results.NotFound();
            }

            return Results.Accepted();
        });

        return app;
    }
}
