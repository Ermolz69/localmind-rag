using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<AiModel> AiModels { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<Bucket> Buckets { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }
    DbSet<DocumentEmbedding> DocumentEmbeddings { get; }
    DbSet<DocumentFile> DocumentFiles { get; }
    DbSet<IngestionJob> IngestionJobs { get; }
    DbSet<LocalDevice> LocalDevices { get; }
    DbSet<Note> Notes { get; }
    DbSet<NoteLink> NoteLinks { get; }
    DbSet<SyncOutboxItem> SyncOutbox { get; }
    DbSet<SyncState> SyncStates { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
