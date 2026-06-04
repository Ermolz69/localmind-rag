namespace LocalMind.Sync.Contracts.Sync;

public sealed record PullRequest(Guid DeviceId, string Cursor, int Limit);
