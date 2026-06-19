using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using Microsoft.Extensions.Caching.Memory;

namespace KnowledgeApp.Infrastructure.Settings;

public sealed class AppSettingsCache(IMemoryCache memoryCache) : IAppSettingsCache
{
    private const string CacheKey = "app-settings";
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<AppSettingsDto> GetOrCreateAsync(
        Func<CancellationToken, Task<AppSettingsDto>> factory,
        CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(CacheKey, out AppSettingsDto? cached)
            && cached is not null)
        {
            return cached;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (memoryCache.TryGetValue(CacheKey, out cached)
                && cached is not null)
            {
                return cached;
            }

            AppSettingsDto settings = await factory(cancellationToken);
            memoryCache.Set(CacheKey, settings);
            return settings;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            memoryCache.Remove(CacheKey);
        }
        finally
        {
            gate.Release();
        }
    }
}
