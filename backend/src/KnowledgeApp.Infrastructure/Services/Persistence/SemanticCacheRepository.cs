using KnowledgeApp.Application.Abstractions.Rag;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Persistence;
using KnowledgeApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Services.Persistence;

public sealed class SemanticCacheRepository(AppDbContext dbContext) : ISemanticCacheRepository
{
    public async Task<SemanticCacheEntry?> FindBestMatchAsync(float[] queryEmbedding, double threshold, CancellationToken ct)
    {
        if (queryEmbedding.Length == 0)
        {
            return null;
        }

        var candidates = await dbContext.SemanticCacheEntries
            .Where(x => x.EmbeddingDimension == queryEmbedding.Length)
            .ToArrayAsync(ct);

        SemanticCacheEntry? bestMatch = null;
        double bestScore = -1;

        foreach (var candidate in candidates)
        {
            if (IsEmptySources(candidate.SourcesJson))
            {
                continue;
            }

            float[] candidateEmbedding = EmbeddingVectorSerializer.FromBytes(candidate.QuestionEmbedding);
            double score = CosineSimilarity(queryEmbedding, candidateEmbedding);
            if (score >= threshold && score > bestScore)
            {
                bestScore = score;
                bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    public async Task AddAsync(SemanticCacheEntry entry, CancellationToken ct)
    {
        dbContext.SemanticCacheEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    private static bool IsEmptySources(string sourcesJson)
    {
        return string.IsNullOrWhiteSpace(sourcesJson)
            || string.Equals(sourcesJson.Trim(), "[]", StringComparison.Ordinal);
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
