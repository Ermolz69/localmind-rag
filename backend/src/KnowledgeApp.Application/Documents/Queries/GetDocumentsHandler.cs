using System.Globalization;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Pagination;
using KnowledgeApp.Application.Common.Errors;
using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Common;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Documents;

public sealed class GetDocumentsHandler(
    IDocumentRepository documentRepository,
    IIngestionJobRepository ingestionJobs)
{
    private const string CursorKind = "documents";

    public async Task<Result<CursorPage<DocumentDto>>> HandleAsync(GetDocumentsQuery query, CancellationToken cancellationToken = default)
    {
        Result<int> limitResult = CursorPagination.ValidateLimit(query.Limit);
        if (!limitResult.IsSuccess)
        {
            return Result<CursorPage<DocumentDto>>.Failure(limitResult.Error!);
        }

        Result<DocumentStatus?> statusResult = ParseStatus(query.Status);
        if (!statusResult.IsSuccess)
        {
            return Result<CursorPage<DocumentDto>>.Failure(statusResult.Error!);
        }

        int limit = limitResult.Value;
        DocumentStatus? status = statusResult.Value;
        string filterHash = CursorPagination.CreateFilterHash(new { query.BucketId, Status = status?.ToString() });
        Result<CursorPayload?> cursorResult = CursorPagination.Decode(query.Cursor, CursorKind, filterHash);
        if (!cursorResult.IsSuccess)
        {
            return Result<CursorPage<DocumentDto>>.Failure(cursorResult.Error!);
        }

        CursorPayload? cursor = cursorResult.Value;

        IReadOnlyList<Document> documentRows = await documentRepository.ListAsync(
            query.BucketId,
            status?.ToString(),
            limit: 0,
            offset: 0,
            cancellationToken);

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
        IReadOnlyList<IngestionJob> failedJobs = await ingestionJobs.GetFailedJobsForDocumentsAsync(documentIds, cancellationToken);
        DocumentDto[] documentDtos = documentPage.Items
            .Select(document => ToDocumentDto(document, failedJobs))
            .ToArray();

        return Result<CursorPage<DocumentDto>>.Success(
            new CursorPage<DocumentDto>(documentDtos, documentPage.NextCursor, documentPage.Limit, documentPage.HasMore));
    }

    private static Result<DocumentStatus?> ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return Result<DocumentStatus?>.Success(null);
        }

        if (Enum.TryParse(status, ignoreCase: true, out DocumentStatus parsedStatus))
        {
            return Result<DocumentStatus?>.Success(parsedStatus);
        }

        return Result<DocumentStatus?>.Failure(ApplicationErrors.Validation(
                ErrorCodes.Documents.InvalidStatus,
                ErrorMessages.Documents.InvalidStatus,
                new Dictionary<string, string[]> { ["status"] = [ErrorMessages.Documents.InvalidStatus] }));
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
            .Select(job => job.ErrorMessage)
            .FirstOrDefault();

        var tags = document.Tags?.Count > 0
            ? (IReadOnlyDictionary<string, string>)document.Tags.ToDictionary(t => t.Key, t => t.Value)
            : null;

        return new DocumentDto(document.Id, document.BucketId, document.Name, document.Status.ToString(), document.CreatedAt, lastError, tags);
    }
}
