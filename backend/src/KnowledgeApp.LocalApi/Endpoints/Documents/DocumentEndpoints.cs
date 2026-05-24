using KnowledgeApp.Application.Documents;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;

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
                HttpContext context,
                CancellationToken cancellationToken) =>
            ApiResults.Ok(await handler.HandleAsync(
                new GetDocumentsQuery(bucketId, status, cursor, limit ?? 50),
                cancellationToken),
                context))
            .WithName("ListDocuments")
            .WithTags("Documents")
            .WithSummary("Lists documents.")
            .WithDescription("Returns a cursor-paged document list filtered by optional bucket and status values.")
            .Produces<ApiResponse<CursorPage<DocumentDto>>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/api/documents/upload", async (
            IFormFile file,
            Guid? bucketId,
            UploadDocumentHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            await using Stream? stream = file.OpenReadStream();
            UploadDocumentResponse? response = await handler.HandleAsync(
                new UploadDocumentCommand(stream, file.FileName, file.ContentType, file.Length, bucketId),
                cancellationToken);

            return ApiResults.Created($"/api/documents/{response.DocumentId}", response, context);
        })
            .DisableAntiforgery()
            .WithName("UploadDocument")
            .WithTags("Documents")
            .WithSummary("Uploads a document.")
            .WithDescription("Stores an uploaded file locally and queues it for ingestion.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ApiResponse<UploadDocumentResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapGet("/api/documents/{id:guid}", async (
            Guid id,
            GetDocumentByIdHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            DocumentDto? document = await handler.HandleAsync(new GetDocumentByIdQuery(id), cancellationToken);
            return document is null
                ? ApiResults.Failure(ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."), context)
                : ApiResults.Ok(document, context);
        })
            .WithName("GetDocument")
            .WithTags("Documents")
            .WithSummary("Gets a document.")
            .WithDescription("Returns document metadata by local document identifier.")
            .Produces<ApiResponse<DocumentDto>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapDelete("/api/documents/{id:guid}", async (
            Guid id,
            DeleteDocumentHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            DeleteDocumentResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return ApiResults.Failure(ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."), context);
            }

            return ApiResults.Empty(context);
        })
            .WithName("DeleteDocument")
            .WithTags("Documents")
            .WithSummary("Deletes a document.")
            .WithDescription("Deletes a document record and hides it from local document queries.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapPost("/api/documents/{id:guid}/reindex", async (
            Guid id,
            ReindexDocumentHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            ReindexDocumentResult result = await handler.HandleAsync(id, cancellationToken);
            if (!result.Found)
            {
                return ApiResults.Failure(ApplicationErrors.NotFound(ErrorCodes.Documents.NotFound, "Document was not found."), context);
            }

            return ApiResults.Accepted(
                $"/api/ingestion/jobs/{result.JobId}",
                new ReindexDocumentResponse(id, result.JobId!.Value, result.Status ?? "Queued"),
                context);
        })
            .WithName("ReindexDocument")
            .WithTags("Documents")
            .WithSummary("Queues document reindexing.")
            .WithDescription("Creates a new ingestion job for an existing document.")
            .Produces<ApiResponse<ReindexDocumentResponse>>(StatusCodes.Status202Accepted)
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        return app;
    }
}
