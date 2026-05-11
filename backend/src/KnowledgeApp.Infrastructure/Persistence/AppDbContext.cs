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
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<NoteLink> NoteLinks => Set<NoteLink>();
    public DbSet<SyncOutboxItem> SyncOutbox => Set<SyncOutboxItem>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(null);
        modelBuilder.Entity<Bucket>().ToTable("buckets");
        modelBuilder.Entity<Document>().ToTable("documents").HasIndex(x => x.BucketId);
        modelBuilder.Entity<DocumentFile>().ToTable("document_files").HasIndex(x => x.DocumentId);
        modelBuilder.Entity<DocumentChunk>().ToTable("document_chunks").HasIndex(x => x.DocumentId);
        modelBuilder.Entity<DocumentEmbedding>().ToTable("document_embeddings").HasIndex(x => x.DocumentChunkId).IsUnique();
        modelBuilder.Entity<Note>().ToTable("notes").HasIndex(x => x.BucketId);
        modelBuilder.Entity<NoteLink>().ToTable("note_links");
        modelBuilder.Entity<Conversation>().ToTable("conversations");
        modelBuilder.Entity<ChatMessage>().ToTable("chat_messages").HasIndex(x => x.ConversationId);
        modelBuilder.Entity<IngestionJob>().ToTable("ingestion_jobs").HasIndex(x => x.DocumentId);
        modelBuilder.Entity<SyncOutboxItem>().ToTable("sync_outbox").HasIndex(x => x.Status);
        modelBuilder.Entity<SyncState>().ToTable("sync_state").HasIndex(x => x.Scope).IsUnique();
        modelBuilder.Entity<AppSetting>().ToTable("app_settings").HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<AiModel>().ToTable("ai_models");
    }
}
