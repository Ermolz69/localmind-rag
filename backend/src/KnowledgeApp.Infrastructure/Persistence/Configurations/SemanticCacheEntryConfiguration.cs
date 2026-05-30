using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class SemanticCacheEntryConfiguration : IEntityTypeConfiguration<SemanticCacheEntry>
{
    public void Configure(EntityTypeBuilder<SemanticCacheEntry> builder)
    {
        builder.ToTable("semantic_cache_entries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Question).IsRequired();
        builder.Property(x => x.QuestionEmbedding).IsRequired();
        builder.Property(x => x.EmbeddingDimension).IsRequired();
        builder.Property(x => x.Answer).IsRequired();
        builder.Property(x => x.SourcesJson).IsRequired();
    }
}
