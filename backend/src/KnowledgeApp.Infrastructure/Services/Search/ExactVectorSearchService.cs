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

public sealed class ExactVectorSearchService(AppDbContext dbContext) : IVectorSearchService, IVectorIndex
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
        float queryNorm = System.Numerics.Tensors.TensorPrimitives.Norm(normalizedQuery);
        if (queryNorm > 0)
        {
            System.Numerics.Tensors.TensorPrimitives.Divide(normalizedQuery, queryNorm, normalizedQuery);
        }

        var rowsQuery =
            from embedding in dbContext.DocumentEmbeddings
            join chunk in dbContext.DocumentChunks on embedding.DocumentChunkId equals chunk.Id
            join document in dbContext.Documents on chunk.DocumentId equals document.Id
            where document.DeletedAt == null
            select new
            {
                DocumentId = document.Id,
                DocumentName = document.Name,
                DocumentBucketId = document.BucketId,
                ChunkId = chunk.Id,
                chunk.PageNumber,
                chunk.Text,
                embedding.Dimension,
                embedding.Embedding,
            };

        if (options.DocumentId is { } documentId)
        {
            rowsQuery = rowsQuery.Where(x => x.DocumentId == documentId);
        }

        if (options.BucketId is { } bucketId)
        {
            rowsQuery = rowsQuery.Where(x => x.DocumentBucketId == bucketId);
        }

        if (options.Tags is { Count: > 0 } tags)
        {
            foreach (var tag in tags)
            {
                rowsQuery = rowsQuery.Where(x =>
                    dbContext.DocumentTags.Any(dt => dt.DocumentId == x.DocumentId && dt.Key == tag.Key && dt.Value == tag.Value) ||
                    dbContext.DocumentChunkTags.Any(ct => ct.DocumentChunkId == x.ChunkId && ct.Key == tag.Key && ct.Value == tag.Value));
            }
        }

        // Use a min-heap to keep track of the top K results without sorting the entire dataset
        var topK = new PriorityQueue<RagSourceDto, double>(options.Limit);

        await foreach (var x in rowsQuery.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (x.Dimension != normalizedQuery.Length)
            {
                continue;
            }

            var span = x.Embedding.AsSpan();
            var chunkVector = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(span);

            // Since embeddings are normalized at ingestion and the query is normalized here,
            // we can use SIMD Dot product which is equivalent to Cosine Similarity.
            double score = System.Numerics.Tensors.TensorPrimitives.Dot(normalizedQuery, chunkVector);

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

        return results;
    }
}
