using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Common.Diagnostics;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.UnitTests.Settings;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task UpdateAsync_Should_Invalidate_Cache_And_Publish_After_Save()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        RecordingSettingsCache cache = new();
        RecordingSettingsChangeSignal signal = new();
        SettingsService service = CreateService(database.Context, cache, signal);

        await service.UpdateAsync(CreateSettings());

        Assert.Equal(1, cache.InvalidationCount);
        Assert.Equal(1, signal.PublishCount);
    }

    [Fact]
    public async Task UpdateAsync_Should_Not_Invalidate_Or_Publish_When_Save_Fails()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        RecordingSettingsCache cache = new();
        RecordingSettingsChangeSignal signal = new();
        SettingsService service = CreateService(
            new ThrowingAppDbContext(database.Context),
            cache,
            signal);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(CreateSettings()));

        Assert.Equal(0, cache.InvalidationCount);
        Assert.Equal(0, signal.PublishCount);
    }

    private static SettingsService CreateService(
        IAppDbContext dbContext,
        RecordingSettingsCache cache,
        RecordingSettingsChangeSignal signal)
    {
        AppSettingsDto defaults = CreateSettings();
        return new SettingsService(
            dbContext,
            new FixedDateTimeProvider(),
            new FakeSettingsDefaultsProvider(defaults),
            new SettingsValidator(new AcceptingWatchedFolderPathValidator()),
            new FakeOperationLogRepository(),
            cache,
            signal);
    }

    private static AppSettingsDto CreateSettings()
    {
        return new AppSettingsDto(
            new AppearanceSettingsDto("System"),
            new AiSettingsDto("LlamaCpp", "chat", "embedding", "runtime", "models"),
            new RuntimePathsSettingsDto("data", "database", "files", "index", "logs"),
            new SyncSettingsDto(false, false),
            new DiagnosticsSettingsDto(true),
            new WatchedFoldersSettingsDto(false, 1000, "MarkDeleted", []));
    }

    private sealed class FakeSettingsDefaultsProvider(AppSettingsDto settings)
        : ISettingsDefaultsProvider
    {
        public AppSettingsDto GetDefaults() => settings;
    }

    private sealed class AcceptingWatchedFolderPathValidator : IWatchedFolderPathValidator
    {
        public IReadOnlyList<string> Validate(
            string path,
            RuntimePathsSettingsDto runtimePaths,
            IReadOnlyList<string> configuredFolderPaths)
        {
            return [];
        }
    }

    private sealed class FakeOperationLogRepository : IOperationLogRepository
    {
        public Task AddAsync(OperationLog log, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OperationLog>> GetRecentLogsAsync(
            int limit,
            string? cursor,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<OperationLog>>([]);
        }
    }

    private sealed class RecordingSettingsCache : IAppSettingsCache
    {
        public int InvalidationCount { get; private set; }

        public Task<AppSettingsDto> GetOrCreateAsync(
            Func<CancellationToken, Task<AppSettingsDto>> factory,
            CancellationToken cancellationToken = default)
        {
            return factory(cancellationToken);
        }

        public Task InvalidateAsync(CancellationToken cancellationToken = default)
        {
            InvalidationCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSettingsChangeSignal : ISettingsChangeSignal
    {
        public int PublishCount { get; private set; }

        public ValueTask<bool> PublishAsync(CancellationToken cancellationToken = default)
        {
            PublishCount++;
            return ValueTask.FromResult(true);
        }

        public ValueTask ReadAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingAppDbContext(IAppDbContext inner) : IAppDbContext
    {
        public DbSet<AiModel> AiModels => inner.AiModels;
        public DbSet<AppSetting> AppSettings => inner.AppSettings;
        public DbSet<Bucket> Buckets => inner.Buckets;
        public DbSet<ChatMessage> ChatMessages => inner.ChatMessages;
        public DbSet<Conversation> Conversations => inner.Conversations;
        public DbSet<Document> Documents => inner.Documents;
        public DbSet<DocumentChunk> DocumentChunks => inner.DocumentChunks;
        public DbSet<DocumentEmbedding> DocumentEmbeddings => inner.DocumentEmbeddings;
        public DbSet<DocumentFile> DocumentFiles => inner.DocumentFiles;
        public DbSet<IngestionJob> IngestionJobs => inner.IngestionJobs;
        public DbSet<LocalDevice> LocalDevices => inner.LocalDevices;
        public DbSet<Note> Notes => inner.Notes;
        public DbSet<NoteFolder> NoteFolders => inner.NoteFolders;
        public DbSet<NoteLink> NoteLinks => inner.NoteLinks;
        public DbSet<SyncOutboxItem> SyncOutbox => inner.SyncOutbox;
        public DbSet<SyncState> SyncStates => inner.SyncStates;
        public DbSet<SemanticCacheEntry> SemanticCacheEntries => inner.SemanticCacheEntries;
        public DbSet<WatchedFileLink> WatchedFileLinks => inner.WatchedFileLinks;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated save failure.");
        }
    }
}
