namespace LocalMind.Sync.Domain.Manifests;

public sealed record SyncManifest(Guid DeviceId, IReadOnlyList<ManifestItem> Items, DateTimeOffset CreatedAt);
