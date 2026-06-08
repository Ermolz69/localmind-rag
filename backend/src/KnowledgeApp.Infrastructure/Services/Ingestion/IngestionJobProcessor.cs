using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Ingestion.IncrementalIndexing;
using KnowledgeApp.Contracts.Documents;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class IngestionJobProcessor(
    AppDbContext dbContext,
    IIngestionJobRepository ingestionJobs,
    IDocumentTextExtractorFactory extractorFactory,
    IDocumentChunker chunker,
    IDocumentEmbeddingService documentEmbeddingService,
    IContentHashService contentHashService,
    IIncrementalChunkPlanner incrementalChunkPlanner,
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

        dbContext.OperationLogs.Add(new OperationLog
        {
            OperationType = "Ingestion.Start",
            EntityType = "IngestionJob",
            EntityId = jobId.ToString(),
            Message = $"Started processing ingestion job for document '{document.Id}'",
            TraceId = operationId.ToString()
        });

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

            if (extractor is null)
            {
                throw new InvalidOperationException("Document file type is not supported.");
            }

            DocumentTextExtractionResult extraction = await extractor.ExtractAsync(documentFile.LocalPath, cancellationToken);

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.TextExtracted,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.DocumentId] = document.Id,
                    [DiagnosticNames.Properties.SegmentsCount] = extraction.Segments.Count
                });

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

            List<ChunkCandidate> incomingChunks = BuildChunkCandidates(extraction);

            if (incomingChunks.Count == 0)
            {
                throw new InvalidOperationException("No extractable text was found in the document.");
            }

            DocumentChunk[] existingChunks = await dbContext.DocumentChunks
                .Where(x => x.DocumentId == document.Id)
                .OrderBy(x => x.Index)
                .ToArrayAsync(cancellationToken);

            Guid[] existingChunkIds = existingChunks
                .Select(x => x.Id)
                .ToArray();

            DocumentEmbedding[] existingEmbeddings = await dbContext.DocumentEmbeddings
                .Where(x => existingChunkIds.Contains(x.DocumentChunkId))
                .ToArrayAsync(cancellationToken);

            ChunkDiffPlan diffPlan = incrementalChunkPlanner.BuildPlan(incomingChunks, existingChunks);

            List<DocumentChunk> newChunks = diffPlan.NewChunks
                .Select(candidate => new DocumentChunk
                {
                    CreatedAt = dateTimeProvider.UtcNow,
                    DocumentId = document.Id,
                    Index = candidate.Index,
                    PageNumber = candidate.PageNumber,
                    Text = candidate.Text,
                    TextHash = candidate.TextHash,
                    ChunkVersion = candidate.ChunkVersion
                })
                .ToList();

            List<DocumentChunkTag> newChunkTags = CreateChunkMetadataTags(diffPlan.NewChunks, newChunks);

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

            EmbeddingCreationResult embeddingCreationResult = await CreateEmbeddingsForNewChunksAsync(
                newChunks,
                document.Id,
                cancellationToken);

            if (await StopIfCancelledAsync(jobId, document, cancellationToken))
            {
                return;
            }

            ApplyReusedChunkUpdates(diffPlan.ReusedChunks);

            HashSet<Guid> deletedChunkIds = diffPlan.DeletedChunks
                .Select(chunk => chunk.Id)
                .ToHashSet();

            DocumentEmbedding[] deletedEmbeddings = existingEmbeddings
                .Where(embedding => deletedChunkIds.Contains(embedding.DocumentChunkId))
                .ToArray();

            dbContext.DocumentEmbeddings.RemoveRange(deletedEmbeddings);
            dbContext.DocumentChunks.RemoveRange(diffPlan.DeletedChunks);

            dbContext.DocumentChunks.AddRange(newChunks);
            dbContext.DocumentChunkTags.AddRange(newChunkTags);
            dbContext.DocumentEmbeddings.AddRange(embeddingCreationResult.Embeddings);

            document.IndexedContentHash = contentHashService.ComputeDocumentHash(
                incomingChunks.Select(chunk => chunk.TextHash),
                IndexingVersions.CurrentDocumentIndexVersion);

            document.IndexVersion = IndexingVersions.CurrentDocumentIndexVersion;
            document.Status = DocumentStatus.Indexed;

            dbContext.OperationLogs.Add(new OperationLog
            {
                OperationType = "Ingestion.Success",
                EntityType = "IngestionJob",
                EntityId = jobId.ToString(),
                Message = $"Ingestion job for document '{document.Id}' completed successfully",
                TraceId = operationId.ToString()
            });

            diagnostics?.LogStep(
                operationId,
                DiagnosticNames.Steps.ChunksAndEmbeddingsCreated,
                new Dictionary<string, object?>
                {
                    [DiagnosticNames.Properties.ChunksCount] = incomingChunks.Count,
                    [DiagnosticNames.Properties.EmbeddingsCount] = embeddingCreationResult.Embeddings.Count,
                    ["reusedChunksCount"] = diffPlan.ReusedChunks.Count,
                    ["newChunksCount"] = newChunks.Count,
                    ["deletedChunksCount"] = diffPlan.DeletedChunks.Count,
                    ["generatedEmbeddingsCount"] = embeddingCreationResult.GeneratedEmbeddingsCount,
                    ["crossDocumentReusedEmbeddingsCount"] = embeddingCreationResult.CrossDocumentReusedEmbeddingsCount
                });

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

            dbContext.OperationLogs.Add(new OperationLog
            {
                OperationType = "Ingestion.Failure",
                EntityType = "IngestionJob",
                EntityId = jobId.ToString(),
                Message = $"Ingestion job for document '{document.Id}' failed: {sanitizedError}",
                TraceId = operationId.ToString()
            });

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
                [DiagnosticNames.Properties.DocumentStatus] = document.Status.ToString()
            });
    }

    private List<ChunkCandidate> BuildChunkCandidates(DocumentTextExtractionResult extraction)
    {
        List<ChunkCandidate> candidates = [];

        foreach (DocumentTextSegment segment in extraction.Segments)
        {
            foreach (DocumentChunkText chunk in chunker.SplitDetailed(segment.Text))
            {
                string chunkText = chunk.Text;
                string textHash = contentHashService.ComputeChunkHash(chunkText);

                candidates.Add(new ChunkCandidate(
                    Index: candidates.Count,
                    PageNumber: segment.PageNumber,
                    Text: chunkText,
                    TextHash: textHash,
                    ChunkVersion: IndexingVersions.CurrentChunkVersion,
                    HeadingPath: chunk.HeadingPath,
                    SourceStartOffset: chunk.SourceStartOffset,
                    SourceEndOffset: chunk.SourceEndOffset));
            }
        }

        return candidates;
    }

    private List<DocumentChunkTag> CreateChunkMetadataTags(
        IReadOnlyList<ChunkCandidate> candidates,
        IReadOnlyList<DocumentChunk> chunks)
    {
        List<DocumentChunkTag> tags = [];

        for (int index = 0; index < candidates.Count && index < chunks.Count; index++)
        {
            ChunkCandidate candidate = candidates[index];
            DocumentChunk chunk = chunks[index];

            AddTag(tags, chunk.Id, "documentId", chunk.DocumentId.ToString());
            AddTag(tags, chunk.Id, "chunkIndex", chunk.Index.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (!string.IsNullOrWhiteSpace(candidate.HeadingPath))
            {
                AddTag(tags, chunk.Id, "headingPath", candidate.HeadingPath);
            }

            if (candidate.SourceStartOffset is not null && candidate.SourceEndOffset is not null)
            {
                AddTag(
                    tags,
                    chunk.Id,
                    "sourceSpan",
                    $"{candidate.SourceStartOffset.Value}:{candidate.SourceEndOffset.Value}");
            }

            if (chunk.PageNumber is not null)
            {
                AddTag(tags, chunk.Id, "pageNumber", chunk.PageNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            AddTag(tags, chunk.Id, "textHash", chunk.TextHash);
        }

        return tags;
    }

    private void AddTag(List<DocumentChunkTag> tags, Guid chunkId, string key, string value)
    {
        tags.Add(new DocumentChunkTag
        {
            CreatedAt = dateTimeProvider.UtcNow,
            DocumentChunkId = chunkId,
            Key = key,
            Value = value
        });
    }

    private async Task<EmbeddingCreationResult> CreateEmbeddingsForNewChunksAsync(
        IReadOnlyList<DocumentChunk> newChunks,
        Guid currentDocumentId,
        CancellationToken cancellationToken)
    {
        if (newChunks.Count == 0)
        {
            return new EmbeddingCreationResult([], 0, 0);
        }

        Dictionary<string, DocumentEmbedding> reusableEmbeddingsByTextHash =
            await LoadReusableEmbeddingsByTextHashAsync(newChunks, currentDocumentId, cancellationToken);

        List<DocumentEmbedding> copiedEmbeddings = [];
        List<DocumentChunk> chunksWithoutReusableEmbeddings = [];

        foreach (DocumentChunk newChunk in newChunks)
        {
            if (reusableEmbeddingsByTextHash.TryGetValue(newChunk.TextHash, out DocumentEmbedding? reusableEmbedding))
            {
                copiedEmbeddings.Add(CopyEmbeddingForChunk(reusableEmbedding, newChunk.Id));
                continue;
            }

            chunksWithoutReusableEmbeddings.Add(newChunk);
        }

        IReadOnlyList<DocumentEmbedding> generatedEmbeddings = chunksWithoutReusableEmbeddings.Count == 0
            ? []
            : await documentEmbeddingService.GenerateAsync(chunksWithoutReusableEmbeddings, cancellationToken);

        List<DocumentEmbedding> allEmbeddings = new List<DocumentEmbedding>(
            copiedEmbeddings.Count + generatedEmbeddings.Count);

        allEmbeddings.AddRange(copiedEmbeddings);
        allEmbeddings.AddRange(generatedEmbeddings);

        return new EmbeddingCreationResult(
            allEmbeddings,
            generatedEmbeddings.Count,
            copiedEmbeddings.Count);
    }

    private async Task<Dictionary<string, DocumentEmbedding>> LoadReusableEmbeddingsByTextHashAsync(
        IReadOnlyList<DocumentChunk> newChunks,
        Guid currentDocumentId,
        CancellationToken cancellationToken)
    {
        string modelName = documentEmbeddingService.ModelName;

        if (string.IsNullOrWhiteSpace(modelName))
        {
            return [];
        }

        string[] textHashes = newChunks
            .Select(chunk => chunk.TextHash)
            .Where(textHash => !string.IsNullOrWhiteSpace(textHash))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (textHashes.Length == 0)
        {
            return [];
        }

        List<ReusableEmbeddingProjection> reusableEmbeddings = await (
            from chunk in dbContext.DocumentChunks.AsNoTracking()
            join embedding in dbContext.DocumentEmbeddings.AsNoTracking()
                on chunk.Id equals embedding.DocumentChunkId
            where chunk.DocumentId != currentDocumentId
                && chunk.ChunkVersion == IndexingVersions.CurrentChunkVersion
                && textHashes.Contains(chunk.TextHash)
                && embedding.ModelName == modelName
            select new ReusableEmbeddingProjection(chunk.TextHash, embedding))
            .ToListAsync(cancellationToken);

        return reusableEmbeddings
            .GroupBy(item => item.TextHash, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(item => item.Embedding.CreatedAt)
                    .First()
                    .Embedding,
                StringComparer.Ordinal);
    }

    private DocumentEmbedding CopyEmbeddingForChunk(DocumentEmbedding sourceEmbedding, Guid targetChunkId)
    {
        return new DocumentEmbedding
        {
            CreatedAt = dateTimeProvider.UtcNow,
            DocumentChunkId = targetChunkId,
            ModelName = sourceEmbedding.ModelName,
            Dimension = sourceEmbedding.Dimension,
            Embedding = sourceEmbedding.Embedding.ToArray()
        };
    }

    private static void ApplyReusedChunkUpdates(IReadOnlyList<ChunkReuseMatch> reusedChunks)
    {
        foreach (ChunkReuseMatch reusedChunk in reusedChunks)
        {
            reusedChunk.ExistingChunk.Index = reusedChunk.Candidate.Index;
            reusedChunk.ExistingChunk.PageNumber = reusedChunk.Candidate.PageNumber;
            reusedChunk.ExistingChunk.Text = reusedChunk.Candidate.Text;
            reusedChunk.ExistingChunk.TextHash = reusedChunk.Candidate.TextHash;
            reusedChunk.ExistingChunk.ChunkVersion = reusedChunk.Candidate.ChunkVersion;
        }
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

    private sealed record EmbeddingCreationResult(
        IReadOnlyList<DocumentEmbedding> Embeddings,
        int GeneratedEmbeddingsCount,
        int CrossDocumentReusedEmbeddingsCount);

    private sealed record ReusableEmbeddingProjection(
        string TextHash,
        DocumentEmbedding Embedding);
}
