using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Persistence;

namespace KnowledgeApp.UnitTests.TestSupport;

internal static class EmbeddedChunkTestData
{
    public static async Task<(Guid DocumentId, Guid ChunkId)> AddEmbeddedChunkAsync(
        AppDbContext context,
        string documentName,
        string chunkText,
        float[] vector,
        Guid? bucketId = null,
        DateTimeOffset? deletedAt = null,
        DateTimeOffset? createdAt = null,
        FileType? fileType = null)
    {
        Document document = new()
        {
            BucketId = bucketId,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
            DeletedAt = deletedAt,
            Name = documentName,
            Status = DocumentStatus.Indexed,
        };
        DocumentChunk chunk = new() { DocumentId = document.Id, Index = 0, Text = chunkText };
        DocumentEmbedding embedding = new()
        {
            DocumentChunkId = chunk.Id,
            ModelName = "test-model",
            Dimension = vector.Length,
            Embedding = ToBytes(vector),
        };

        context.Documents.Add(document);
        context.DocumentChunks.Add(chunk);
        context.DocumentEmbeddings.Add(embedding);
        if (fileType.HasValue)
        {
            context.DocumentFiles.Add(new DocumentFile
            {
                DocumentId = document.Id,
                FileName = documentName,
                LocalPath = $"runtime/app/files/{document.Id:N}",
                ContentHash = document.Id.ToString("N"),
                FileType = fileType.Value,
                SizeBytes = chunkText.Length,
            });
        }

        await context.SaveChangesAsync();

        return (document.Id, chunk.Id);
    }

    private static byte[] ToBytes(float[] vector)
    {
        byte[] bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }
}
