using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class WatchedFileLinkConfiguration : IEntityTypeConfiguration<WatchedFileLink>
{
    public void Configure(EntityTypeBuilder<WatchedFileLink> builder)
    {
        builder.ToTable("watched_file_links");

        builder.Property(link => link.WatchedFolderPath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(link => link.FilePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(link => link.NormalizedFilePath)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(link => link.LastContentHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(link => link.DocumentId);

        builder.HasIndex(link => link.WatchedFolderPath);

        builder.HasIndex(link => link.NormalizedFilePath)
            .IsUnique();
    }
}
