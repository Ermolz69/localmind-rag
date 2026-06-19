using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Settings;

public interface IAppSettingsCache
{
    Task<AppSettingsDto> GetOrCreateAsync(
        Func<CancellationToken, Task<AppSettingsDto>> factory,
        CancellationToken cancellationToken = default);

    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
