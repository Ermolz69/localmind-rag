using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class DocumentTagConfiguration : IEntityTypeConfiguration<DocumentTag>
{
    public void Configure(EntityTypeBuilder<DocumentTag> builder)
    {
        builder.ToTable("document_tags");
        builder.HasIndex(tag => tag.DocumentId);
        builder.HasIndex(tag => new { tag.Key, tag.Value });

        builder.HasOne<Document>()
               .WithMany(d => d.Tags)
               .HasForeignKey(tag => tag.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
