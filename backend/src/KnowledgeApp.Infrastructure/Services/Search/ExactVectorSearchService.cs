using System.Runtime.InteropServices;
using System.Numerics.Tensors;
using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Search;
using KnowledgeApp.Contracts.Rag;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Infrastructure.Services;

public sealed class ExactVectorSearchService(
    AppDbContext dbContext,
    ILogger<ExactVectorSearchService> logger) : IVectorSearchService, IVectorIndex
{
    public Task UpsertAsync(Guid chunkId, float[] vector, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<IReadOnlyList<RagSourceDto>> SearchAsync(float[] queryVector, VectorSearchOptions options, CancellationToken cancellationToken = default)
    {
        if (queryVector.Length == 0 || options.Limit <= 0)
        {
            return [];
        }

        // Normalize the query vector once
        float[] normalizedQuery = queryVector.ToArray();
        float queryNorm = TensorPrimitives.Norm(normalizedQuery);
        if (queryNorm <= float.Epsilon)
        {
            logger.LogWarning("Skipping exact vector search because query embedding norm is zero.");
            return [];
        }

        TensorPrimitives.Divide(normalizedQuery, queryNorm, normalizedQuery);

        var candidateQuery =
            from embedding in dbContext.DocumentEmbeddings.AsNoTracking()
            join chunk in dbContext.DocumentChunks.AsNoTracking() on embedding.DocumentChunkId equals chunk.Id
            join document in dbContext.Documents.AsNoTracking() on chunk.DocumentId equals document.Id
            where document.DeletedAt == null
            select new
            {
                Document = document,
                Chunk = chunk,
                Embedding = embedding,
            };

        if (options.DocumentId is { } documentId)
        {
            candidateQuery = candidateQuery.Where(x => x.Document.Id == documentId);
        }

        if (options.BucketId is { } bucketId)
        {
            candidateQuery = candidateQuery.Where(x => x.Document.BucketId == bucketId);
        }

        if (options.DateFrom.HasValue)
        {
            long dateFromUnixTime = SearchDateIndexing.ToUnixTimeMilliseconds(options.DateFrom.Value);
            candidateQuery = candidateQuery.Where(x =>
                EF.Property<long>(x.Document, SearchDateIndexing.CreatedAtUnixTimePropertyName) >= dateFromUnixTime);
        }

        if (options.DateTo.HasValue)
        {
            long dateToUnixTime = SearchDateIndexing.ToUnixTimeMilliseconds(
                SearchDateRange.ToInclusiveEndOfDay(options.DateTo.Value));
            candidateQuery = candidateQuery.Where(x =>
                EF.Property<long>(x.Document, SearchDateIndexing.CreatedAtUnixTimePropertyName) <= dateToUnixTime);
        }

        if (options.FileType is { } fileType)
        {
            KnowledgeApp.Domain.Enums.FileType parsedFileType = FileTypeParser.Parse(fileType);
            candidateQuery = candidateQuery.Where(x =>
                dbContext.DocumentFiles.Any(df => df.DocumentId == x.Document.Id && df.FileType == parsedFileType));
        }

        if (options.Tags is { Count: > 0 } tags)
        {
            foreach (var tag in tags)
            {
                candidateQuery = candidateQuery.Where(x =>
                    dbContext.DocumentTags.Any(dt => dt.DocumentId == x.Document.Id && dt.Key == tag.Key && dt.Value == tag.Value) ||
                    dbContext.DocumentChunkTags.Any(ct => ct.DocumentChunkId == x.Chunk.Id && ct.Key == tag.Key && ct.Value == tag.Value));
            }
        }

        var rowsQuery = candidateQuery.Select(x => new
        {
            DocumentId = x.Document.Id,
            DocumentName = x.Document.Name,
            ChunkId = x.Chunk.Id,
            x.Chunk.PageNumber,
            x.Chunk.Text,
            x.Embedding.Dimension,
            x.Embedding.Embedding,
        });

        // Use a min-heap to keep track of the top K results without sorting the entire dataset
        var topK = new PriorityQueue<RagSourceDto, double>(options.Limit);
        int skippedCorruptedCount = 0;

        await foreach (var x in rowsQuery.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (x.Dimension != normalizedQuery.Length)
            {
                skippedCorruptedCount++;
                continue;
            }

            var span = x.Embedding.AsSpan();
            if (span.Length % sizeof(float) != 0 || span.Length / sizeof(float) != x.Dimension)
            {
                skippedCorruptedCount++;
                continue;
            }

            var chunkVector = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(span);

            // Since embeddings are normalized at ingestion and the query is normalized here,
            // we can use SIMD Dot product which is equivalent to Cosine Similarity.
            double score = TensorPrimitives.Dot(normalizedQuery, chunkVector);

            if (!double.IsFinite(score))
            {
                skippedCorruptedCount++;
                continue;
            }

            if (topK.Count < options.Limit)
            {
                topK.Enqueue(new RagSourceDto(x.DocumentId, x.DocumentName, x.ChunkId, x.PageNumber, score, x.Text), score);
            }
            else if (topK.TryPeek(out _, out double minScore) && score > minScore)
            {
                topK.Dequeue();
                topK.Enqueue(new RagSourceDto(x.DocumentId, x.DocumentName, x.ChunkId, x.PageNumber, score, x.Text), score);
            }
        }

        var results = new RagSourceDto[topK.Count];
        for (int i = results.Length - 1; i >= 0; i--)
        {
            results[i] = topK.Dequeue();
        }

        if (skippedCorruptedCount > 0)
        {
            logger.LogWarning(
                "Skipped {SkippedCount} corrupted or dimension-mismatched vector blobs during exact search. " +
                "Existing local dev databases should be deleted or re-indexed after SIMD optimizations.",
                skippedCorruptedCount);
        }

        return results;
    }
}
