using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class DocumentEmbeddingConfiguration : IEntityTypeConfiguration<DocumentEmbedding>
{
    public void Configure(EntityTypeBuilder<DocumentEmbedding> builder)
    {
        builder.ToTable("document_embeddings");
        builder.HasIndex(embedding => embedding.DocumentChunkId).IsUnique();
    }
}
