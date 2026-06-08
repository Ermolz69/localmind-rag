namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;

public enum WatchedFileFilterReason
{
    Allowed = 0,
    MissingFile,
    UnsupportedExtension,
    ExtensionNotAllowed,
    IgnoredFolder,
    IgnoredPattern,
    FileTooLarge,
    InvalidPath
}
