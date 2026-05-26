using KnowledgeApp.Application.Documents;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;

namespace KnowledgeApp.LocalApi.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/documents", async (
            Guid? bucketId,
            string? status,
            string? cursor,
            int? limit,
            GetDocumentsHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(
                new GetDocumentsQuery(bucketId, status, cursor, limit ?? 50),
                cancellationToken)).ToApiResult(context))
            .WithName("ListDocuments")
            .WithTags("Documents")
            .WithSummary("Lists documents.")
            .WithDescription("Returns a cursor-paged document list filtered by optional bucket and status values.")
            .Produces<ApiResponse<CursorPage<DocumentDto>>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapPost("/documents/upload", async (
            IFormFile file,
            Guid? bucketId,
            UploadDocumentHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            await using Stream? stream = file.OpenReadStream();

            return (await handler.HandleAsync(
                new UploadDocumentCommand(stream, file.FileName, file.ContentType, file.Length, bucketId),
                cancellationToken))
                .ToCreatedApiResult(context, response => $"{ApiVersions.V1Prefix}/documents/{response.DocumentId}");
        })
            .DisableAntiforgery()
            .WithName("UploadDocument")
            .WithTags("Documents")
            .WithSummary("Uploads a document.")
            .WithDescription("Stores an uploaded file locally and queues it for ingestion.")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ApiResponse<UploadDocumentResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<object?>>(StatusCodes.Status400BadRequest);

        app.MapGet("/documents/{id:guid}", async (
            Guid id,
            GetDocumentByIdHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(new GetDocumentByIdQuery(id), cancellationToken)).ToApiResult(context);
        })
            .WithName("GetDocument")
            .WithTags("Documents")
            .WithSummary("Gets a document.")
            .WithDescription("Returns document metadata by local document identifier.")
            .Produces<ApiResponse<DocumentDto>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapDelete("/documents/{id:guid}", async (
            Guid id,
            DeleteDocumentHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, cancellationToken)).ToApiResult(context);
        })
            .WithName("DeleteDocument")
            .WithTags("Documents")
            .WithSummary("Deletes a document.")
            .WithDescription("Deletes a document record and hides it from local document queries.")
            .Produces<ApiResponse<object?>>()
            .Produces<ApiResponse<object?>>(StatusCodes.Status404NotFound);

        app.MapPost("/documents/{id:guid}/reindex", async (
            Guid id,
            ReindexDocumentHandler handler,
            HttpContext context,
            CancellationToken cancellationToken) =>
        {
            return (await handler.HandleAsync(id, cancellationToken))
                .ToAcceptedApiResult(
                    context,
                    response => $"{ApiVersions.V1Prefix}/ingestion/jobs/{response.IngestionJobId}");
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
