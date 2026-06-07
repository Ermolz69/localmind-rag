using KnowledgeApp.Application.Abstractions;
using KnowledgeApp.Application.Ingestion.WatchedFolders;
using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Contracts.WatchedFolders;
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

            FakeSettingsService fakeSettingsService = new FakeSettingsService(new AppSettingsDto(
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
                loggerFactory.CreateLogger<WindowsFileWatcherHostedService>()
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

    private sealed class FakeReconciliationService : IWatchedFolderReconciliationService
    {
        public List<WatchedFolderDto> ReconciledFolders { get; } = new();

        public Task ReconcileFolderAsync(WatchedFolderDto folder, CancellationToken cancellationToken = default)
        {
            ReconciledFolders.Add(folder);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        private readonly AppSettingsDto settings;

        public FakeSettingsService(AppSettingsDto settings)
        {
            this.settings = settings;
        }

        public Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(settings);
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

        public void AddOrUpdate(WatchedFileChange change) { }

        public IReadOnlyList<WatchedFileChange> DequeueReadyChanges(DateTimeOffset now, TimeSpan debounceDelay)
        {
            return Array.Empty<WatchedFileChange>();
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
    }

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public FakeDateTimeProvider(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }
}
