using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public interface IWatchedFolderPathValidator
{
    IReadOnlyList<string> Validate(
        string path,
        RuntimePathsSettingsDto runtimePaths,
        IReadOnlyList<string> configuredFolderPaths);
}
