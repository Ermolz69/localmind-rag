using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.Property(chunk => chunk.ChunkType)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue("unknown");

        builder.Property(chunk => chunk.TokenizerId)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        builder.Property(chunk => chunk.ChunkingAlgorithmId)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        builder.Property(chunk => chunk.ChunkIdentityHash)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        builder.Property(chunk => chunk.EmbeddingTextHash)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        builder.Property(chunk => chunk.ChunkVersion)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(chunk => chunk.DocumentId);

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.Index });

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkIdentityHash });

        builder.HasIndex(chunk => chunk.EmbeddingTextHash);

        builder.HasIndex(chunk => new { chunk.EmbeddingTextHash, chunk.ChunkVersion });

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkVersion });

        builder.HasIndex(chunk => chunk.ChunkType);

        builder.HasIndex(chunk => chunk.TokenizerId);
    }
}
