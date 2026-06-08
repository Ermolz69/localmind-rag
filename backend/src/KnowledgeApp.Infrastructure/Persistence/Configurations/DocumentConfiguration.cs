using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.Property(document => document.IndexedContentHash)
            .HasMaxLength(64);

        builder.Property(document => document.IndexVersion)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(document => document.BucketId);

        builder.HasIndex(document => document.DeletedAt);

        builder.HasIndex(document => document.LocalDeviceId);

        builder.HasIndex(document => document.IndexedContentHash);
    }
}
