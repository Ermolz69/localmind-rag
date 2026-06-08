using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkTagConfiguration : IEntityTypeConfiguration<DocumentChunkTag>
{
    public void Configure(EntityTypeBuilder<DocumentChunkTag> builder)
    {
        builder.ToTable("document_chunk_tags");
        builder.HasIndex(tag => tag.DocumentChunkId);
        builder.HasIndex(tag => new { tag.Key, tag.Value });

        builder.HasOne<DocumentChunk>()
               .WithMany()
               .HasForeignKey(tag => tag.DocumentChunkId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
