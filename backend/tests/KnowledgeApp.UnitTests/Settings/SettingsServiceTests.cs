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
    public async Task GetAsync_Should_Return_Default_Developer_Diagnostics_Settings()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        SettingsService service = CreateService(
            database.Context,
            new RecordingSettingsCache(),
            new RecordingSettingsChangeSignal());

        AppSettingsDto settings = await service.GetAsync();

        Assert.NotNull(settings.Diagnostics);
        Assert.False(settings.Diagnostics.DeveloperModeEnabled);
        Assert.Equal("Information", settings.Diagnostics.MinimumLogLevel);
        Assert.False(settings.Diagnostics.UseSeparateLogFiles);
        Assert.True(settings.Diagnostics.EnableErrorLogs);
        Assert.False(settings.Diagnostics.EnableSqlLogs);
        Assert.True(settings.Diagnostics.EnableHttpLogs);
        Assert.False(settings.Diagnostics.EnableDiagnosticEventLogs);
        Assert.False(settings.Diagnostics.EnableDebugTrace);
    }

    [Fact]
    public async Task UpdateAsync_Should_Persist_Developer_Diagnostics_Settings()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        RecordingLogSettingsApplier logSettings = new();
        SettingsService service = CreateService(
            database.Context,
            new RecordingSettingsCache(),
            new RecordingSettingsChangeSignal(),
            logSettings);

        AppSettingsDto request = CreateSettings() with
        {
            Diagnostics = new DiagnosticsSettingsDto(
                Enabled: true,
                DeveloperModeEnabled: true,
                MinimumLogLevel: "Debug",
                UseSeparateLogFiles: true,
                EnableErrorLogs: false,
                EnableSqlLogs: true,
                EnableHttpLogs: false,
                EnableDiagnosticEventLogs: true,
                EnableDebugTrace: true),
        };

        await service.UpdateAsync(request);

        Dictionary<string, string> stored = await database.Context.AppSettings
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value);

        Assert.Equal("True", stored[SettingsKeys.DiagnosticsDeveloperModeEnabled]);
        Assert.Equal("Debug", stored[SettingsKeys.DiagnosticsMinimumLogLevel]);
        Assert.Equal("True", stored[SettingsKeys.DiagnosticsUseSeparateLogFiles]);
        Assert.Equal("False", stored[SettingsKeys.DiagnosticsEnableErrorLogs]);
        Assert.Equal("True", stored[SettingsKeys.DiagnosticsEnableSqlLogs]);
        Assert.Equal("False", stored[SettingsKeys.DiagnosticsEnableHttpLogs]);
        Assert.Equal("True", stored[SettingsKeys.DiagnosticsEnableDiagnosticEventLogs]);
        Assert.Equal("True", stored[SettingsKeys.DiagnosticsEnableDebugTrace]);
        Assert.NotNull(logSettings.LastSettings);
        Assert.True(logSettings.LastSettings.UseSeparateLogFiles);
        Assert.False(logSettings.LastSettings.EnableErrorLogs);
        Assert.True(logSettings.LastSettings.EnableSqlLogs);
    }

    [Fact]
    public async Task GetAsync_Should_Default_Companion_Mode_To_Disabled()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        SettingsService service = CreateService(
            database.Context,
            new RecordingSettingsCache(),
            new RecordingSettingsChangeSignal());

        AppSettingsDto settings = await service.GetAsync();

        Assert.NotNull(settings.CompanionMode);
        Assert.False(settings.CompanionMode.Enabled);
    }

    [Fact]
    public async Task UpdateAsync_Should_Persist_Companion_Mode_Enabled()
    {
        await using ApplicationTestDatabase database = await ApplicationTestDatabase.CreateAsync();
        SettingsService service = CreateService(
            database.Context,
            new RecordingSettingsCache(),
            new RecordingSettingsChangeSignal());

        AppSettingsDto request = CreateSettings() with
        {
            CompanionMode = new CompanionModeSettingsDto(Enabled: true),
        };

        await service.UpdateAsync(request);

        Dictionary<string, string> stored = await database.Context.AppSettings
            .ToDictionaryAsync(setting => setting.Key, setting => setting.Value);

        Assert.Equal("True", stored[SettingsKeys.CompanionModeEnabled]);

        AppSettingsDto reloaded = await service.GetAsync();
        Assert.NotNull(reloaded.CompanionMode);
        Assert.True(reloaded.CompanionMode.Enabled);
    }

    [Fact]
    public void SettingsValidator_Should_Reject_Unsupported_Log_Level()
    {
        SettingsValidator validator = new(new AcceptingWatchedFolderPathValidator());
        AppSettingsDto request = CreateSettings() with
        {
            Diagnostics = new DiagnosticsSettingsDto(
                Enabled: true,
                MinimumLogLevel: "Chatty"),
        };

        KnowledgeApp.Application.Common.Results.ApplicationError error =
            validator.Validate(request).AssertFailure(KnowledgeApp.Application.Common.Results.ErrorType.Validation);

        Assert.Contains(error.Details ?? [], detail => detail.Field == "diagnostics.minimumLogLevel");
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
            signal,
            new RecordingLogSettingsApplier());
    }

    private static SettingsService CreateService(
        IAppDbContext dbContext,
        RecordingSettingsCache cache,
        RecordingSettingsChangeSignal signal,
        ILogSettingsApplier logSettingsApplier)
    {
        AppSettingsDto defaults = CreateSettings();
        return new SettingsService(
            dbContext,
            new FixedDateTimeProvider(),
            new FakeSettingsDefaultsProvider(defaults),
            new SettingsValidator(new AcceptingWatchedFolderPathValidator()),
            new FakeOperationLogRepository(),
            cache,
            signal,
            logSettingsApplier);
    }

    private static AppSettingsDto CreateSettings()
    {
        return new AppSettingsDto(
            new AppearanceSettingsDto("System"),
            new AiSettingsDto("LlamaCpp", "chat", "embedding", "runtime", "models"),
            new RuntimePathsSettingsDto("data", "database", "files", "index", "logs"),
            new SyncSettingsDto(false, false),
            new DiagnosticsSettingsDto(
                Enabled: true,
                DeveloperModeEnabled: false,
                MinimumLogLevel: "Information",
                UseSeparateLogFiles: false,
                EnableErrorLogs: true,
                EnableSqlLogs: false,
                EnableHttpLogs: true,
                EnableDiagnosticEventLogs: false,
                EnableDebugTrace: false),
            new WatchedFoldersSettingsDto(false, 1000, "MarkDeleted", []),
            new CompanionModeSettingsDto(false));
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

    private sealed class RecordingLogSettingsApplier : ILogSettingsApplier
    {
        public DiagnosticsSettingsDto? LastSettings { get; private set; }

        public void Apply(DiagnosticsSettingsDto settings)
        {
            LastSettings = settings;
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
