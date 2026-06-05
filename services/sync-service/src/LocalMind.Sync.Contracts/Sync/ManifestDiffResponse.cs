namespace LocalMind.Sync.Contracts.Sync;

public sealed record ManifestDiffResponse(
    Guid DeviceId,
    IReadOnlyList<ManifestItemDto> MissingRemote,
    IReadOnlyList<ManifestItemDto> MissingLocal,
    IReadOnlyList<ManifestItemDto> Diverged);
