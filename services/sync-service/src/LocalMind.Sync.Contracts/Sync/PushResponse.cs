namespace LocalMind.Sync.Contracts.Sync;

public sealed record PushResponse(Guid DeviceId, int AcceptedChanges, string QueueMessageId);
