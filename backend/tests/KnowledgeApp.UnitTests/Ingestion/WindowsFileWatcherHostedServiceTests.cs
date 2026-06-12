using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Contracts.WatchedFolders;
using KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;
using KnowledgeApp.Infrastructure.Services.WatchedFolders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.UnitTests.Ingestion;

public class WindowsFileWatcherHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Should_NotReconcileDisabledFolder()
    {
        // Arrange
        using CancellationTokenSource cts = new CancellationTokenSource();
        string tempDirectory = Path.Combine(Path.GetTempPath(), "localmind-rag-test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            ServiceCollection services = new ServiceCollection();
            FakeReconciliationService fakeReconciliationService = new FakeReconciliationService();
            services.AddSingleton<IWatchedFolderReconciliationService>(fakeReconciliationService);

            FakeWatchedFileIngestionService fakeIngestionService = new FakeWatchedFileIngestionService();
            services.AddSingleton<IWatchedFileIngestionService>(fakeIngestionService);

            FakeSettingsService fakeSettingsService = new FakeSettingsService(new AppSettingsDto(
                default!,
                default!,
                default!,
                default!,
                default!,
                new WatchedFoldersSettingsDto(
                    Enabled: true,
                    DebounceMilliseconds: 100,
                    DeletePolicy: "MarkDeleted",
                    Folders: new[]
                    {
                        new WatchedFolderDto(tempDirectory, IncludeSubdirectories: true, Enabled: false) // DISABLED
                    }
                )
            ));
            services.AddSingleton<ISettingsService>(fakeSettingsService);

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            FakeScopeFactory scopeFactory = new FakeScopeFactory(serviceProvider);
            FakeFileWatcherDebounceBuffer debounceBuffer = new FakeFileWatcherDebounceBuffer();
            FakeWatchedFolderStatusStore statusStore = new FakeWatchedFolderStatusStore();
            FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider(DateTimeOffset.UtcNow);
            LoggerFactory loggerFactory = new LoggerFactory();

            WindowsFileWatcherHostedService hostedService = new WindowsFileWatcherHostedService(
                scopeFactory,
                debounceBuffer,
                statusStore,
                dateTimeProvider,
                new FakeWatchedFileFilterService(),
                new KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager(new FakeApplicationLifetime(), new LoggerFactory().CreateLogger<KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager>()), loggerFactory.CreateLogger<WindowsFileWatcherHostedService>()
            );

            // Act
            Task executeTask = hostedService.StartAsync(cts.Token);

            // Allow background loop to run a bit to trigger the initial sync
            await Task.Delay(1000);

            cts.Cancel();
            try { await executeTask; } catch (OperationCanceledException) { }

            // Assert
            Assert.Empty(fakeReconciliationService.ReconciledFolders);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ProcessReadyChangesAsync_Should_DelegateCreatedOrChangedToIngestionService()
    {
        // Arrange
        using CancellationTokenSource cts = new CancellationTokenSource();
        ServiceCollection services = new ServiceCollection();

        FakeReconciliationService fakeReconciliationService = new FakeReconciliationService();
        services.AddSingleton<IWatchedFolderReconciliationService>(fakeReconciliationService);

        FakeWatchedFileIngestionService fakeIngestionService = new FakeWatchedFileIngestionService();
        services.AddSingleton<IWatchedFileIngestionService>(fakeIngestionService);

        FakeSettingsService fakeSettingsService = new FakeSettingsService(new AppSettingsDto(
            default!, default!, default!, default!, default!,
            new WatchedFoldersSettingsDto(Enabled: false, DebounceMilliseconds: 0, DeletePolicy: "MarkDeleted", Folders: [])
        ));
        services.AddSingleton<ISettingsService>(fakeSettingsService);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FakeScopeFactory scopeFactory = new FakeScopeFactory(serviceProvider);

        FakeFileWatcherDebounceBuffer debounceBuffer = new FakeFileWatcherDebounceBuffer();
        // Setup debounce buffer to return exactly one CreatedOrChanged event
        debounceBuffer.ReadyChangesToReturn = new List<WatchedFileChange>
        {
            new WatchedFileChange("C:\\test\\doc.txt", "C:\\test", WatchedFileChangeType.CreatedOrChanged, DateTimeOffset.UtcNow)
        };

        FakeWatchedFolderStatusStore statusStore = new FakeWatchedFolderStatusStore();
        FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider(DateTimeOffset.UtcNow);
        LoggerFactory loggerFactory = new LoggerFactory();

        WindowsFileWatcherHostedService hostedService = new WindowsFileWatcherHostedService(
            scopeFactory, debounceBuffer, statusStore, dateTimeProvider, new FakeWatchedFileFilterService(), new KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager(new FakeApplicationLifetime(), new LoggerFactory().CreateLogger<KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager>()), loggerFactory.CreateLogger<WindowsFileWatcherHostedService>()
        );

        // Act
        Task executeTask = hostedService.StartAsync(cts.Token);
        await Task.Delay(500); // Allow background loop to process the ready change
        cts.Cancel();
        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert
        Assert.Single(fakeIngestionService.CreatedOrChangedCalls);
        Assert.Empty(fakeIngestionService.DeletedCalls);

        var call = fakeIngestionService.CreatedOrChangedCalls[0];
        Assert.Equal("C:\\test\\doc.txt", call.FilePath);
        Assert.Equal("C:\\test", call.WatchedFolderPath);
    }

    [Fact]
    public async Task ProcessReadyChangesAsync_Should_DelegateDeletedToIngestionService()
    {
        // Arrange
        using CancellationTokenSource cts = new CancellationTokenSource();
        ServiceCollection services = new ServiceCollection();

        FakeReconciliationService fakeReconciliationService = new FakeReconciliationService();
        services.AddSingleton<IWatchedFolderReconciliationService>(fakeReconciliationService);

        FakeWatchedFileIngestionService fakeIngestionService = new FakeWatchedFileIngestionService();
        services.AddSingleton<IWatchedFileIngestionService>(fakeIngestionService);

        FakeSettingsService fakeSettingsService = new FakeSettingsService(new AppSettingsDto(
            default!, default!, default!, default!, default!,
            new WatchedFoldersSettingsDto(Enabled: false, DebounceMilliseconds: 0, DeletePolicy: "MarkDeleted", Folders: [])
        ));
        services.AddSingleton<ISettingsService>(fakeSettingsService);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FakeScopeFactory scopeFactory = new FakeScopeFactory(serviceProvider);

        FakeFileWatcherDebounceBuffer debounceBuffer = new FakeFileWatcherDebounceBuffer();
        // Setup debounce buffer to return exactly one Deleted event
        debounceBuffer.ReadyChangesToReturn = new List<WatchedFileChange>
        {
            new WatchedFileChange("C:\\test\\doc.txt", "C:\\test", WatchedFileChangeType.Deleted, DateTimeOffset.UtcNow)
        };

        FakeWatchedFolderStatusStore statusStore = new FakeWatchedFolderStatusStore();
        FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider(DateTimeOffset.UtcNow);
        LoggerFactory loggerFactory = new LoggerFactory();

        WindowsFileWatcherHostedService hostedService = new WindowsFileWatcherHostedService(
            scopeFactory, debounceBuffer, statusStore, dateTimeProvider, new FakeWatchedFileFilterService(), new KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager(new FakeApplicationLifetime(), new LoggerFactory().CreateLogger<KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager>()), loggerFactory.CreateLogger<WindowsFileWatcherHostedService>()
        );

        // Act
        Task executeTask = hostedService.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();
        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert
        Assert.Single(fakeIngestionService.DeletedCalls);
        Assert.Empty(fakeIngestionService.CreatedOrChangedCalls);

        var call = fakeIngestionService.DeletedCalls[0];
        Assert.Equal("C:\\test\\doc.txt", call);
    }

    [Fact]
    public async Task ProcessReadyChangesAsync_Should_DelegateRenamedToIngestionService()
    {
        // Arrange
        using CancellationTokenSource cts = new CancellationTokenSource();
        ServiceCollection services = new ServiceCollection();

        FakeReconciliationService fakeReconciliationService = new FakeReconciliationService();
        services.AddSingleton<IWatchedFolderReconciliationService>(fakeReconciliationService);

        FakeWatchedFileIngestionService fakeIngestionService = new FakeWatchedFileIngestionService();
        services.AddSingleton<IWatchedFileIngestionService>(fakeIngestionService);

        FakeSettingsService fakeSettingsService = new FakeSettingsService(new AppSettingsDto(
            default!, default!, default!, default!, default!,
            new WatchedFoldersSettingsDto(Enabled: false, DebounceMilliseconds: 0, DeletePolicy: "MarkDeleted", Folders: [])
        ));
        services.AddSingleton<ISettingsService>(fakeSettingsService);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        FakeScopeFactory scopeFactory = new FakeScopeFactory(serviceProvider);

        FakeFileWatcherDebounceBuffer debounceBuffer = new FakeFileWatcherDebounceBuffer();
        // Setup debounce buffer to return exactly one Renamed event
        debounceBuffer.ReadyChangesToReturn = new List<WatchedFileChange>
        {
            new WatchedFileChange("C:\\test\\new_doc.txt", "C:\\test", WatchedFileChangeType.Renamed, DateTimeOffset.UtcNow, "C:\\test\\old_doc.txt")
        };

        FakeWatchedFolderStatusStore statusStore = new FakeWatchedFolderStatusStore();
        FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider(DateTimeOffset.UtcNow);
        LoggerFactory loggerFactory = new LoggerFactory();

        WindowsFileWatcherHostedService hostedService = new WindowsFileWatcherHostedService(
            scopeFactory, debounceBuffer, statusStore, dateTimeProvider, new FakeWatchedFileFilterService(), new KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager(new FakeApplicationLifetime(), new LoggerFactory().CreateLogger<KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager>()), loggerFactory.CreateLogger<WindowsFileWatcherHostedService>()
        );

        // Act
        Task executeTask = hostedService.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();
        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert
        Assert.Single(fakeIngestionService.RenamedCalls);
        Assert.Empty(fakeIngestionService.CreatedOrChangedCalls);
        Assert.Empty(fakeIngestionService.DeletedCalls);

        var call = fakeIngestionService.RenamedCalls[0];
        Assert.Equal("C:\\test\\old_doc.txt", call.OldFilePath);
        Assert.Equal("C:\\test\\new_doc.txt", call.NewFilePath);
        Assert.Equal("C:\\test", call.WatchedFolderPath);
    }

    [Fact]
    public async Task ExecuteAsync_Should_TriggerReconciliation_WhenNewFolderAdded()
    {
        // Arrange
        using CancellationTokenSource cts = new CancellationTokenSource();
        string tempDirectory = Path.Combine(Path.GetTempPath(), "localmind-rag-test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            ServiceCollection services = new ServiceCollection();
            FakeReconciliationService fakeReconciliationService = new FakeReconciliationService();
            services.AddSingleton<IWatchedFolderReconciliationService>(fakeReconciliationService);

            FakeWatchedFileIngestionService fakeIngestionService = new FakeWatchedFileIngestionService();
            services.AddSingleton<IWatchedFileIngestionService>(fakeIngestionService);

            // Initially no folders
            FakeSettingsService fakeSettingsService = new FakeSettingsService(new AppSettingsDto(
                default!, default!, default!, default!, default!,
                new WatchedFoldersSettingsDto(Enabled: true, DebounceMilliseconds: 100, DeletePolicy: "MarkDeleted", Folders: [])
            ));
            services.AddSingleton<ISettingsService>(fakeSettingsService);

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            FakeScopeFactory scopeFactory = new FakeScopeFactory(serviceProvider);
            FakeFileWatcherDebounceBuffer debounceBuffer = new FakeFileWatcherDebounceBuffer();
            FakeWatchedFolderStatusStore statusStore = new FakeWatchedFolderStatusStore();
            FakeDateTimeProvider dateTimeProvider = new FakeDateTimeProvider(DateTimeOffset.UtcNow);
            LoggerFactory loggerFactory = new LoggerFactory();

            WindowsFileWatcherHostedService hostedService = new WindowsFileWatcherHostedService(
                scopeFactory, debounceBuffer, statusStore, dateTimeProvider, new FakeWatchedFileFilterService(), new KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager(new FakeApplicationLifetime(), new LoggerFactory().CreateLogger<KnowledgeApp.Infrastructure.Services.Runtime.RuntimeProcessManager>()), loggerFactory.CreateLogger<WindowsFileWatcherHostedService>()
            );

            // Act
            Task executeTask = hostedService.StartAsync(cts.Token);
            await Task.Delay(500);

            Assert.Empty(fakeReconciliationService.ReconciledFolders);

            // Dynamically change settings to include a new valid folder
            // hosted service reloads every 5 seconds, so we need to advance time or wait.
            // Since we use FakeDateTimeProvider, we can advance time
            fakeSettingsService.CurrentSettings = new AppSettingsDto(
                default!, default!, default!, default!, default!,
                new WatchedFoldersSettingsDto(Enabled: true, DebounceMilliseconds: 100, DeletePolicy: "MarkDeleted", Folders: new[]
                {
                    new WatchedFolderDto(tempDirectory, IncludeSubdirectories: true, Enabled: true)
                })
            );
            dateTimeProvider.UtcNow = dateTimeProvider.UtcNow.AddSeconds(6);

            await Task.Delay(500); // Allow loop to run again and catch the new settings

            cts.Cancel();
            try { await executeTask; } catch (OperationCanceledException) { }

            // Assert
            Assert.Single(fakeReconciliationService.ReconciledFolders);
            Assert.Equal(tempDirectory, fakeReconciliationService.ReconciledFolders[0].Path);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private sealed class FakeReconciliationService : IWatchedFolderReconciliationService
    {
        public List<WatchedFolderDto> ReconciledFolders { get; } = new();

        public Task<WatchedFolderReconciliationResult> ReconcileFolderAsync(WatchedFolderDto folder, CancellationToken cancellationToken = default)
        {
            ReconciledFolders.Add(folder);
            return Task.FromResult(new WatchedFolderReconciliationResult(0, 0, 0, 0));
        }
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public AppSettingsDto CurrentSettings { get; set; }

        public FakeSettingsService(AppSettingsDto settings)
        {
            this.CurrentSettings = settings;
        }

        public Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CurrentSettings);
        }

        public Task<KnowledgeApp.Application.Common.Results.Result> UpdateAsync(AppSettingsDto request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KnowledgeApp.Application.Common.Results.Result.Success());
        }
    }

    private sealed class FakeScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceProvider serviceProvider;

        public FakeScopeFactory(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IServiceScope CreateScope()
        {
            return new FakeScope(serviceProvider);
        }
    }

    private sealed class FakeScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; }

        public FakeScope(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public void Dispose() { }
    }

    private sealed class FakeFileWatcherDebounceBuffer : IFileWatcherDebounceBuffer
    {
        public int PendingCount => 0;

        public IReadOnlyList<WatchedFileChange> ReadyChangesToReturn { get; set; } = Array.Empty<WatchedFileChange>();

        public void AddOrUpdate(WatchedFileChange change) { }

        public IReadOnlyList<WatchedFileChange> DequeueReadyChanges(DateTimeOffset now, TimeSpan debounceDelay)
        {
            var changes = ReadyChangesToReturn;
            ReadyChangesToReturn = Array.Empty<WatchedFileChange>(); // only return once
            return changes;
        }
    }

    private sealed class FakeWatchedFileIngestionService : IWatchedFileIngestionService
    {
        public List<(string FilePath, string WatchedFolderPath)> CreatedOrChangedCalls { get; } = new();
        public List<string> DeletedCalls { get; } = new();
        public List<(string OldFilePath, string NewFilePath, string WatchedFolderPath)> RenamedCalls { get; } = new();

        public Task HandleCreatedOrChangedAsync(string filePath, string watchedFolderPath, CancellationToken cancellationToken = default)
        {
            CreatedOrChangedCalls.Add((filePath, watchedFolderPath));
            return Task.CompletedTask;
        }

        public Task HandleDeletedAsync(string filePath, CancellationToken cancellationToken = default)
        {
            DeletedCalls.Add(filePath);
            return Task.CompletedTask;
        }

        public Task HandleRenamedAsync(string oldFilePath, string newFilePath, string watchedFolderPath, CancellationToken cancellationToken = default)
        {
            RenamedCalls.Add((oldFilePath, newFilePath, watchedFolderPath));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWatchedFolderStatusStore : IWatchedFolderStatusStore
    {
        public WatchedFolderStatusResponse GetStatus(WatchedFoldersSettingsDto settings)
        {
            return new WatchedFolderStatusResponse(true, 0, 0, "MarkDeleted", null, null, new List<WatchedFolderStatusDto>());
        }

        public void SetFolderWatching(string folderPath, bool isWatching) { }
        public void SetFolderPendingEvents(string folderPath, int pendingEvents) { }
        public void RecordFolderEvent(string folderPath, DateTimeOffset occurredAt) { }
        public void RecordFolderError(string folderPath, string sanitizedError, DateTimeOffset occurredAt) { }
        public void RecordGlobalError(string sanitizedError, DateTimeOffset occurredAt) { }
        public void SetGlobalPendingEvents(int pendingEvents) { }
        public void RecordScanStarted(string folderPath, DateTimeOffset occurredAt) { }
        public void RecordScanCompleted(string folderPath, DateTimeOffset occurredAt, WatchedFolderReconciliationResult result) { }
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; set; }
    }

    private sealed class FakeWatchedFileFilterService : IWatchedFileFilterService
    {
        public WatchedFileFilterContext CreateContext(WatchedFoldersSettingsDto settings)
        {
            return new WatchedFileFilterContext(settings);
        }

        public WatchedFileFilterResult Evaluate(string filePath, WatchedFileFilterContext context)
        {
            return WatchedFileFilterResult.Allowed();
        }
    }
}
