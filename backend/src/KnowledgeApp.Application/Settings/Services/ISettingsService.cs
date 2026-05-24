using KnowledgeApp.Application.Common.Results;
using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Settings;

public interface ISettingsService
{
    Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(AppSettingsDto request, CancellationToken cancellationToken = default);
}
