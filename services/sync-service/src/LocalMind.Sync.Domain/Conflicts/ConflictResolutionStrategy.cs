namespace LocalMind.Sync.Domain.Conflicts;

public enum ConflictResolutionStrategy
{
    KeepLocal = 1,
    KeepRemote = 2,
    Merge = 3
}
