using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeApp.Infrastructure.Persistence.Configurations;

public sealed class SyncStateConfiguration : IEntityTypeConfiguration<SyncState>
{
    public void Configure(EntityTypeBuilder<SyncState> builder)
    {
        builder.ToTable("sync_state");
        builder.HasIndex(state => state.Scope).IsUnique();
    }
}
