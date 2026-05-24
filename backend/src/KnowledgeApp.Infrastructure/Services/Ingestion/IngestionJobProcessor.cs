using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Contracts.Runtime;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using A = DocumentFormat.OpenXml.Drawing;
using PresentationSlideId = DocumentFormat.OpenXml.Presentation.SlideId;
using SlideText = DocumentFormat.OpenXml.Drawing.Text;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class IngestionJobProcessor(
    AppDbContext dbContext,
    IIngestionJobRepository ingestionJobs,
    IDocumentTextExtractorFactory extractorFactory,
    IDocumentChunker chunker,
    IDocumentEmbeddingService documentEmbeddingService,
    IDateTimeProvider dateTimeProvider,
    IAppDiagnosticLogger? diagnostics = null) : IIngestionJobProcessor
{
    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        Guid operationId = diagnostics?.BeginOperation(
            DiagnosticNames.Areas.Ingestion,
            DiagnosticNames.Operations.IngestionProcessJob,
            new Dictionary<string, object?> { [DiagnosticNames.Properties.JobId] = jobId }) ?? Guid.Empty;

        DateTimeOffset now = dateTimeProvider.UtcNow;
        IngestionJob? job = await ingestionJobs.ClaimForProcessingAsync(jobId, operationId, now, cancellationToken);
        if (job is null)
        {
            IngestionJob? existingJob = await ingestionJobs.GetAsync(jobId, cancellationToken);
            diagnostics?.LogStep(
                operationId,
                existingJob is null ? DiagnosticNames.Steps.JobNotFound : DiagnosticNames.Steps.JobSkipped,
                existingJob is null
                    ? null
                    : new Dictionary<string, object?> { [DiagnosticNames.Properties.Status] = existingJob.Status.ToString() });
            return;
        }

        Document? document = await dbContext.Documents.FindAsync([job.DocumentId], cancellationToken);
        if (document is null)
        {
            await ingestionJobs.MarkFailedAsync(
                jobId,
                "INGESTION_JOB_FAILED",
                "Document was not found.",
                dateTimeProvider.UtcNow,
                cancellationToken);
            diagnostics?.LogStep(operationId, DiagnosticNames.Steps.DocumentNotFound);
            return;
        }

        document.Status = DocumentStatus.Processing;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            DocumentFile? documentFile = await dbContext.DocumentFiles
                .Where(x => x.DocumentId == document.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (documentFile is null)
            {
                throw new InvalidOperationException("Document file was not found.");
            }

            await ingestionJobs.UpdateStepAsync(
                jobId,
                IngestionJobStatus.Processing,
                "Extracting text",
                30,
                dateTimeProvider.UtcNow,
                cancellationToken);
            if (await StopIfCancelledAsync(jobId, document, cancellationToken))
            {
                return;
            }

            string? extension = Path.GetExtension(documentFile.FileName);
            IDocumentTextExtractor? extractor = extractorFactory.GetExtractor(documentFile.FileType, extension, null);
            DocumentTextExtractionResult? extraction = await extractor.ExtractAsync(documentFile.LocalPath, cancellationToken);
            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.TextExtracted,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.DocumentId] = document.Id,
                    [DiagnosticNames.Properties.SegmentsCount] = extraction.Segments.Count,
                });
            DocumentChunk[]? existingChunks = await dbContext.DocumentChunks
                .Where(x => x.DocumentId == document.Id)
                .ToArrayAsync(cancellationToken);
            Guid[]? existingChunkIds = existingChunks.Select(x => x.Id).ToArray();
            DocumentEmbedding[]? existingEmbeddings = await dbContext.DocumentEmbeddings
                .Where(x => existingChunkIds.Contains(x.DocumentChunkId))
                .ToArrayAsync(cancellationToken);

            await ingestionJobs.UpdateStepAsync(
                jobId,
                IngestionJobStatus.Chunking,
                "Chunking document",
                50,
                dateTimeProvider.UtcNow,
                cancellationToken);
            if (await StopIfCancelledAsync(jobId, document, cancellationToken))
            {
                return;
            }

            dbContext.DocumentEmbeddings.RemoveRange(existingEmbeddings);
            dbContext.DocumentChunks.RemoveRange(existingChunks);
            List<DocumentChunk>? newChunks = new List<DocumentChunk>();
            foreach (DocumentTextSegment segment in extraction.Segments)
            {
                foreach (string chunkText in chunker.Split(segment.Text))
                {
                    newChunks.Add(new DocumentChunk
                    {
                        CreatedAt = dateTimeProvider.UtcNow,
                        DocumentId = document.Id,
                        Index = newChunks.Count,
                        PageNumber = segment.PageNumber,
                        Text = chunkText,
                    });
                }
            }

            if (newChunks.Count == 0)
            {
                throw new InvalidOperationException("No extractable text was found in the document.");
            }

            dbContext.DocumentChunks.AddRange(newChunks);
            await ingestionJobs.UpdateStepAsync(
                jobId,
                IngestionJobStatus.Embedding,
                "Generating embeddings",
                75,
                dateTimeProvider.UtcNow,
                cancellationToken);
            if (await StopIfCancelledAsync(jobId, document, cancellationToken))
            {
                return;
            }

            IReadOnlyList<DocumentEmbedding>? newEmbeddings = await documentEmbeddingService.GenerateAsync(newChunks, cancellationToken);
            dbContext.DocumentEmbeddings.AddRange(newEmbeddings);
            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.ChunksAndEmbeddingsCreated,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.ChunksCount] = newChunks.Count,
                    [DiagnosticNames.Properties.EmbeddingsCount] = newEmbeddings.Count,
                });

            if (await StopIfCancelledAsync(jobId, document, cancellationToken))
            {
                return;
            }

            document.Status = DocumentStatus.Indexed;
            await dbContext.SaveChangesAsync(cancellationToken);
            await ingestionJobs.MarkIndexedAsync(jobId, dateTimeProvider.UtcNow, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            document.Status = DocumentStatus.Queued;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            await ingestionJobs.UpdateStepAsync(
                jobId,
                IngestionJobStatus.Pending,
                "Pending",
                0,
                dateTimeProvider.UtcNow,
                CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            string sanitizedError = SanitizeIngestionError(exception);
            await ingestionJobs.MarkFailedAsync(
                jobId,
                "INGESTION_JOB_FAILED",
                sanitizedError,
                dateTimeProvider.UtcNow,
                cancellationToken);
            document.Status = DocumentStatus.Failed;
            diagnostics?.LogFailure(operationId, exception);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        IngestionJob? finishedJob = await ingestionJobs.GetAsync(jobId, cancellationToken);
        diagnostics?.LogStep(
            operationId,
            DiagnosticNames.Steps.JobFinished,
            new Dictionary<string, object?>
            {
                [DiagnosticNames.Properties.Status] = finishedJob?.Status.ToString(),
                [DiagnosticNames.Properties.DocumentStatus] = document.Status.ToString(),
            });
    }

    private async Task<bool> StopIfCancelledAsync(Guid jobId, Document document, CancellationToken cancellationToken)
    {
        IngestionJob? currentJob = await ingestionJobs.GetAsync(jobId, cancellationToken);
        if (currentJob?.Status != IngestionJobStatus.Cancelled)
        {
            return false;
        }

        dbContext.ChangeTracker.Clear();
        Document? currentDocument = await dbContext.Documents.FindAsync([document.Id], cancellationToken);
        if (currentDocument is not null)
        {
            currentDocument.Status = DocumentStatus.Queued;
            currentDocument.UpdatedAt = dateTimeProvider.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private static string SanitizeIngestionError(Exception exception)
    {
        string message = exception.Message;
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Document ingestion failed.";
        }

        string[] safeTerms = ["PDF", "DOCX", "PPTX", "extractable text", "Document file"];
        return safeTerms.Any(term => message.Contains(term, StringComparison.OrdinalIgnoreCase))
            ? message
            : "Document ingestion failed.";
    }
}
