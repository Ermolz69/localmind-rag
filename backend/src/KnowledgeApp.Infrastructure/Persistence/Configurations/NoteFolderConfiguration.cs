using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class NoteFolderConfiguration : IEntityTypeConfiguration<NoteFolder>
{
    public void Configure(EntityTypeBuilder<NoteFolder> builder)
    {
        builder.ToTable("note_folders");

        builder.HasIndex(folder => folder.BucketId);
        builder.HasIndex(folder => folder.ParentFolderId);
        builder.HasIndex(folder => folder.DeletedAt);
        builder.HasIndex(folder => folder.LocalDeviceId);
    }
}
