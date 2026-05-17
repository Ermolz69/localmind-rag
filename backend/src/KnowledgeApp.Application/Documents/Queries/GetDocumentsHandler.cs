using System.Globalization;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Pagination;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class GetDocumentsHandler(IAppDbContext dbContext)
{
    private const string CursorKind = "documents";

    public async Task<CursorPage<DocumentDto>> HandleAsync(GetDocumentsQuery query, CancellationToken cancellationToken = default)
    {
        int limit = CursorPagination.ValidateLimit(query.Limit);
        DocumentStatus? status = ParseStatus(query.Status);
        string filterHash = CursorPagination.CreateFilterHash(new { query.BucketId, Status = status?.ToString() });
        CursorPayload? cursor = CursorPagination.Decode(query.Cursor, CursorKind, filterHash);

        IQueryable<Document> documents = dbContext.Documents
            .AsNoTracking()
            .Where(document => document.DeletedAt == null);

        if (query.BucketId.HasValue)
        {
            documents = documents.Where(document => document.BucketId == query.BucketId.Value);
        }

        if (status.HasValue)
        {
            documents = documents.Where(document => document.Status == status.Value);
        }

        Document[] documentRows = await documents
            .ToArrayAsync(cancellationToken);
        Document[] sortedDocuments = documentRows
            .OrderByDescending(document => document.CreatedAt)
            .ThenByDescending(document => document.Id.ToString("N", CultureInfo.InvariantCulture))
            .ToArray();
        CursorPage<Document> documentPage = CursorPagination.CreatePage(
            sortedDocuments,
            cursor,
            limit,
            CompareDocumentToCursor,
            document => new CursorPayload(
                CursorKind,
                filterHash,
                PrimaryDate: null,
                document.CreatedAt,
                document.Id,
                HasPrimaryDate: false));

        Guid[] documentIds = documentPage.Items.Select(document => document.Id).ToArray();
        IngestionJob[] failedJobs = await dbContext.IngestionJobs
            .AsNoTracking()
            .Where(job => documentIds.Contains(job.DocumentId) && job.LastError != null)
            .ToArrayAsync(cancellationToken);
        DocumentDto[] documentDtos = documentPage.Items
            .Select(document => ToDocumentDto(document, failedJobs))
            .ToArray();

        return new CursorPage<DocumentDto>(documentDtos, documentPage.NextCursor, documentPage.Limit, documentPage.HasMore);
    }

    private static DocumentStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (Enum.TryParse(status, ignoreCase: true, out DocumentStatus parsedStatus))
        {
            return parsedStatus;
        }

        throw new ValidationAppException(
            "documents.invalidStatus",
            "Document status filter is invalid.",
            new Dictionary<string, string[]> { ["status"] = ["Document status filter is invalid."] });
    }

    private static int CompareDocumentToCursor(Document document, CursorPayload cursor)
    {
        if (document.Id == cursor.Id)
        {
            return 2;
        }

        if (document.CreatedAt < cursor.CreatedAt)
        {
            return 1;
        }

        if (document.CreatedAt == cursor.CreatedAt &&
            string.Compare(
                document.Id.ToString("N", CultureInfo.InvariantCulture),
                cursor.Id.ToString("N", CultureInfo.InvariantCulture),
                StringComparison.Ordinal) < 0)
        {
            return 1;
        }

        return 0;
    }

    private static DocumentDto ToDocumentDto(Document document, IReadOnlyList<IngestionJob> failedJobs)
    {
        string? lastError = failedJobs
            .Where(job => job.DocumentId == document.Id)
            .OrderByDescending(job => job.ProcessedAt ?? job.CreatedAt)
            .Select(job => job.LastError)
            .FirstOrDefault();

        return new DocumentDto(document.Id, document.Name, document.Status.ToString(), document.CreatedAt, lastError);
    }
}
