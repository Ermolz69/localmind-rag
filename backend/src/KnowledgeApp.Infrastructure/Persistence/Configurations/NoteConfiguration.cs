using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");
        builder.Property<long>(SearchDateIndexing.CreatedAtUnixTimePropertyName)
            .HasColumnType("INTEGER");
        builder.HasIndex(note => note.BucketId);
        builder.HasIndex(SearchDateIndexing.CreatedAtUnixTimePropertyName);
        builder.HasIndex(note => note.DeletedAt);
        builder.HasIndex(note => note.LocalDeviceId);
    }
}
