using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;

public interface IWatchedFileFilterService
{
    WatchedFileFilterContext CreateContext(WatchedFoldersSettingsDto settings);
    WatchedFileFilterResult Evaluate(string filePath, WatchedFileFilterContext context);
}
