using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<AiModel> AiModels => Set<AiModel>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Bucket> Buckets => Set<Bucket>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<DocumentEmbedding> DocumentEmbeddings => Set<DocumentEmbedding>();
    public DbSet<DocumentFile> DocumentFiles => Set<DocumentFile>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<LocalDevice> LocalDevices => Set<LocalDevice>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteFolder> NoteFolders => Set<NoteFolder>();
    public DbSet<NoteLink> NoteLinks => Set<NoteLink>();
    public DbSet<SyncOutboxItem> SyncOutbox => Set<SyncOutboxItem>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();
    public DbSet<SemanticCacheEntry> SemanticCacheEntries => Set<SemanticCacheEntry>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();
    public DbSet<DocumentChunkTag> DocumentChunkTags => Set<DocumentChunkTag>();
    public DbSet<NoteTag> NoteTags => Set<NoteTag>();
    public DbSet<WatchedFileLink> WatchedFileLinks => Set<WatchedFileLink>();
    public DbSet<CompanionDevice> CompanionDevices => Set<CompanionDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(null);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SyncSearchDateIndexes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SyncSearchDateIndexes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SyncSearchDateIndexes()
    {
        foreach (var entry in ChangeTracker.Entries<Document>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            entry.Property(SearchDateIndexing.CreatedAtUnixTimePropertyName).CurrentValue =
                SearchDateIndexing.ToUnixTimeMilliseconds(entry.Entity.CreatedAt);
        }

        foreach (var entry in ChangeTracker.Entries<Note>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            entry.Property(SearchDateIndexing.CreatedAtUnixTimePropertyName).CurrentValue =
                SearchDateIndexing.ToUnixTimeMilliseconds(entry.Entity.CreatedAt);
        }

        foreach (var entry in ChangeTracker.Entries<IngestionJob>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            entry.Property(SearchDateIndexing.CreatedAtUnixTimePropertyName).CurrentValue =
                SearchDateIndexing.ToUnixTimeMilliseconds(entry.Entity.CreatedAt);
        }
    }
}
