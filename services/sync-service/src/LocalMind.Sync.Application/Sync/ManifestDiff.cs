namespace LocalMind.Sync.Application.Sync;

using LocalMind.Sync.Domain.Manifests;

public sealed record ManifestDiff(
    IReadOnlyList<ManifestItem> MissingRemote,
    IReadOnlyList<ManifestItem> MissingLocal,
    IReadOnlyList<ManifestItem> Diverged);
