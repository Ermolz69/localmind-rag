using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class NoteTagConfiguration : IEntityTypeConfiguration<NoteTag>
{
    public void Configure(EntityTypeBuilder<NoteTag> builder)
    {
        builder.ToTable("note_tags");
        builder.HasIndex(tag => tag.NoteId);
        builder.HasIndex(tag => new { tag.Key, tag.Value });

        builder.HasOne<Note>()
               .WithMany()
               .HasForeignKey(tag => tag.NoteId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
