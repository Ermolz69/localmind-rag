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

        var rows = await rowsQuery.ToArrayAsync(cancellationToken);

        return rows
            .Where(x => x.Dimension == queryVector.Length)
            .Select(x =>
            {
                float[]? chunkVector = EmbeddingVectorSerializer.FromBytes(x.Embedding);
                double score = CosineSimilarity(queryVector, chunkVector);
                return new RagSourceDto(x.DocumentId, x.DocumentName, x.ChunkId, x.PageNumber, score, x.Text);
            })
            .OrderByDescending(x => x.Score)
            .Take(options.Limit)
            .ToArray();
    }

    private static double CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || left.Length != right.Length)
        {
            return 0;
        }

        double dotProduct = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (int i = 0; i < left.Length; i++)
        {
            dotProduct += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
