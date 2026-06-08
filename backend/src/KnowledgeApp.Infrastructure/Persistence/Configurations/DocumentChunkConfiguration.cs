using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");

        builder.Property(chunk => chunk.TextHash)
            .IsRequired()
            .HasMaxLength(64)
            .HasDefaultValue(string.Empty);

        builder.Property(chunk => chunk.ChunkVersion)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(chunk => chunk.DocumentId);

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.TextHash });

        builder.HasIndex(chunk => chunk.TextHash);
    }
}
