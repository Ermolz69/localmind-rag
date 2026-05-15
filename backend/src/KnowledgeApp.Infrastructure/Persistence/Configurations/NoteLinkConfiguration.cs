using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class NoteLinkConfiguration : IEntityTypeConfiguration<NoteLink>
{
    public void Configure(EntityTypeBuilder<NoteLink> builder)
    {
        builder.ToTable("note_links");
    }
}
