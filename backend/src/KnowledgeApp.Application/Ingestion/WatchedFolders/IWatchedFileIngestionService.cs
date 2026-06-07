namespace KnowledgeApp.Application.Ingestion.WatchedFolders;

public interface IWatchedFileIngestionService
{
    Task HandleCreatedOrChangedAsync(
        string filePath,
        string watchedFolderPath,
        CancellationToken cancellationToken = default);

    Task HandleDeletedAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    Task HandleRenamedAsync(
        string oldFilePath,
        string newFilePath,
        string watchedFolderPath,
        CancellationToken cancellationToken = default);
}
