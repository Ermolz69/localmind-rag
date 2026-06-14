using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Infrastructure.Settings;
using Microsoft.Extensions.Caching.Memory;

namespace KnowledgeApp.UnitTests.Settings;

public sealed class AppSettingsCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_Should_Load_Only_Once_For_Repeated_Calls()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        AppSettingsCache cache = new(memoryCache);
        AppSettingsDto settings = CreateSettings();
        int loadCount = 0;

        AppSettingsDto first = await cache.GetOrCreateAsync(LoadAsync);
        AppSettingsDto second = await cache.GetOrCreateAsync(LoadAsync);

        Assert.Same(settings, first);
        Assert.Same(settings, second);
        Assert.Equal(1, loadCount);

        Task<AppSettingsDto> LoadAsync(CancellationToken _)
        {
            loadCount++;
            return Task.FromResult(settings);
        }
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_Load_Only_Once_For_Concurrent_Calls()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        AppSettingsCache cache = new(memoryCache);
        AppSettingsDto settings = CreateSettings();
        TaskCompletionSource loadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int loadCount = 0;

        Task<AppSettingsDto>[] requests = Enumerable.Range(0, 8)
            .Select(_ => cache.GetOrCreateAsync(LoadAsync))
            .ToArray();

        await loadStarted.Task;
        releaseLoad.SetResult();
        AppSettingsDto[] results = await Task.WhenAll(requests);

        Assert.All(results, result => Assert.Same(settings, result));
        Assert.Equal(1, loadCount);

        async Task<AppSettingsDto> LoadAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref loadCount);
            loadStarted.TrySetResult();
            await releaseLoad.Task.WaitAsync(cancellationToken);
            return settings;
        }
    }

    [Fact]
    public async Task InvalidateAsync_Should_Force_The_Next_Call_To_Reload()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        AppSettingsCache cache = new(memoryCache);
        int loadCount = 0;

        await cache.GetOrCreateAsync(LoadAsync);
        await cache.InvalidateAsync();
        await cache.GetOrCreateAsync(LoadAsync);

        Assert.Equal(2, loadCount);

        Task<AppSettingsDto> LoadAsync(CancellationToken _)
        {
            loadCount++;
            return Task.FromResult(CreateSettings());
        }
    }

    private static AppSettingsDto CreateSettings()
    {
        return new AppSettingsDto(
            new AppearanceSettingsDto("System"),
            new AiSettingsDto("LlamaCpp", "chat", "embedding", "runtime", "models"),
            new RuntimePathsSettingsDto("data", "database", "files", "index", "logs"),
            new SyncSettingsDto(false, false));
    }
}
