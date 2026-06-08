namespace KnowledgeApp.Application.Ingestion.WatchedFolders.Filtering;

public sealed record WatchedFileFilterResult(
    bool IsAllowed,
    WatchedFileFilterReason Reason)
{
    public static WatchedFileFilterResult Allowed()
        => new(true, WatchedFileFilterReason.Allowed);

    public static WatchedFileFilterResult Rejected(WatchedFileFilterReason reason)
        => new(false, reason);
}
