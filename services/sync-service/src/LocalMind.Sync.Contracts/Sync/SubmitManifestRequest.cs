namespace LocalMind.Sync.Contracts.Sync;

public sealed record SubmitManifestRequest(Guid DeviceId, IReadOnlyList<ManifestItemDto> Items);
