using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class SyncOutboxItemConfiguration : IEntityTypeConfiguration<SyncOutboxItem>
{
    public void Configure(EntityTypeBuilder<SyncOutboxItem> builder)
    {
        builder.ToTable("sync_outbox");
        builder.HasIndex(item => item.Status);
    }
}
