using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using KnowledgeApp.Application.Abstractions;
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
    IDocumentTextExtractorFactory extractorFactory,
    IDocumentChunker chunker,
    IDocumentEmbeddingService documentEmbeddingService,
    IDateTimeProvider dateTimeProvider) : IIngestionJobProcessor
{
    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        IngestionJob? queuedJob = await dbContext.IngestionJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);
        if (queuedJob is null)
        {
            throw new InvalidOperationException("Ingestion job was not found.");
        }

        if (queuedJob.Status != IngestionJobStatus.Queued)
        {
            return;
        }

        DateTimeOffset now = dateTimeProvider.UtcNow;
        int claimed = await dbContext.IngestionJobs
            .Where(job => job.Id == jobId && job.Status == IngestionJobStatus.Queued)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, IngestionJobStatus.Running)
                    .SetProperty(job => job.UpdatedAt, now),
                cancellationToken);

        if (claimed == 0)
        {
            return;
        }

        IngestionJob job = await dbContext.IngestionJobs.SingleAsync(x => x.Id == jobId, cancellationToken);
        Document? document = await dbContext.Documents.FindAsync([job.DocumentId], cancellationToken);
        if (document is null)
        {
            job.Status = IngestionJobStatus.Failed;
            job.LastError = "Document was not found.";
            job.ProcessedAt = dateTimeProvider.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
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

            string? extension = Path.GetExtension(documentFile.FileName);
            IDocumentTextExtractor? extractor = extractorFactory.GetExtractor(documentFile.FileType, extension, null);
            DocumentTextExtractionResult? extraction = await extractor.ExtractAsync(documentFile.LocalPath, cancellationToken);
            DocumentChunk[]? existingChunks = await dbContext.DocumentChunks
                .Where(x => x.DocumentId == document.Id)
                .ToArrayAsync(cancellationToken);
            Guid[]? existingChunkIds = existingChunks.Select(x => x.Id).ToArray();
            DocumentEmbedding[]? existingEmbeddings = await dbContext.DocumentEmbeddings
                .Where(x => existingChunkIds.Contains(x.DocumentChunkId))
                .ToArrayAsync(cancellationToken);

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
            IReadOnlyList<DocumentEmbedding>? newEmbeddings = await documentEmbeddingService.GenerateAsync(newChunks, cancellationToken);
            dbContext.DocumentEmbeddings.AddRange(newEmbeddings);

            job.Status = IngestionJobStatus.Completed;
            job.LastError = null;
            job.ProcessedAt = dateTimeProvider.UtcNow;
            document.Status = DocumentStatus.Indexed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            job.Status = IngestionJobStatus.Queued;
            job.LastError = null;
            job.ProcessedAt = null;
            document.Status = DocumentStatus.Queued;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            job.Status = IngestionJobStatus.Failed;
            job.LastError = exception.Message;
            job.ProcessedAt = dateTimeProvider.UtcNow;
            document.Status = DocumentStatus.Failed;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
