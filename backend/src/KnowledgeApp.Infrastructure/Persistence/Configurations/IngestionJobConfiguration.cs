using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.ToTable("ingestion_jobs");
        builder.Property<long>(SearchDateIndexing.CreatedAtUnixTimePropertyName)
            .HasColumnType("INTEGER");
        builder.HasIndex(job => job.DocumentId);
        builder.HasIndex(
            nameof(IngestionJob.Status),
            SearchDateIndexing.CreatedAtUnixTimePropertyName,
            nameof(IngestionJob.Id));
    }
}
