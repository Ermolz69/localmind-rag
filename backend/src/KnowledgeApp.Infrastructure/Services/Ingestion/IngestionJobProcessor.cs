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
        var job = await dbContext.IngestionJobs.FindAsync([jobId], cancellationToken);
        if (job is null)
        {
            throw new InvalidOperationException("Ingestion job was not found.");
        }

        if (job.Status != IngestionJobStatus.Queued)
        {
            return;
        }

        var document = await dbContext.Documents.FindAsync([job.DocumentId], cancellationToken);
        if (document is null)
        {
            job.Status = IngestionJobStatus.Failed;
            job.LastError = "Document was not found.";
            job.ProcessedAt = dateTimeProvider.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        job.Status = IngestionJobStatus.Running;
        document.Status = DocumentStatus.Processing;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var documentFile = await dbContext.DocumentFiles
                .Where(x => x.DocumentId == document.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (documentFile is null)
            {
                throw new InvalidOperationException("Document file was not found.");
            }

            var extension = Path.GetExtension(documentFile.FileName);
            var extractor = extractorFactory.GetExtractor(documentFile.FileType, extension, null);
            var extraction = await extractor.ExtractAsync(documentFile.LocalPath, cancellationToken);
            var existingChunks = await dbContext.DocumentChunks
                .Where(x => x.DocumentId == document.Id)
                .ToArrayAsync(cancellationToken);
            var existingChunkIds = existingChunks.Select(x => x.Id).ToArray();
            var existingEmbeddings = await dbContext.DocumentEmbeddings
                .Where(x => existingChunkIds.Contains(x.DocumentChunkId))
                .ToArrayAsync(cancellationToken);

            dbContext.DocumentEmbeddings.RemoveRange(existingEmbeddings);
            dbContext.DocumentChunks.RemoveRange(existingChunks);
            var newChunks = new List<DocumentChunk>();
            foreach (var segment in extraction.Segments)
            {
                foreach (var chunkText in chunker.Split(segment.Text))
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
            var newEmbeddings = await documentEmbeddingService.GenerateAsync(newChunks, cancellationToken);
            dbContext.DocumentEmbeddings.AddRange(newEmbeddings);

            job.Status = IngestionJobStatus.Completed;
            job.LastError = null;
            job.ProcessedAt = dateTimeProvider.UtcNow;
            document.Status = DocumentStatus.Indexed;
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
