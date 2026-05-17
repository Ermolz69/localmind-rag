using KnowledgeApp.Application.Settings;
using KnowledgeApp.Contracts.Settings;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Infrastructure.Settings;

public sealed class SettingsDefaultsProvider(
    IOptions<LocalRuntimeOptions> runtimeOptions,
    IOptions<AiOptions> aiOptions) : ISettingsDefaultsProvider
{
    public AppSettingsDto GetDefaults()
    {
        LocalRuntimeOptions? runtime = runtimeOptions.Value;
        AiOptions? ai = aiOptions.Value;

        return new AppSettingsDto(
            Appearance: new AppearanceSettingsDto(AppTheme.System.ToString()),
            Ai: new AiSettingsDto(
                ai.Provider,
                ai.ChatModel,
                ai.EmbeddingModel,
                ai.RuntimePath,
                ai.ModelsPath),
            RuntimePaths: new RuntimePathsSettingsDto(
                runtime.DataPath,
                runtime.DatabasePath,
                runtime.FilesPath,
                runtime.IndexPath,
                runtime.LogsPath),
            Sync: new SyncSettingsDto(false, false));
    }
}
