namespace LocalMind.Sync.Application.Sync;

using LocalMind.Sync.Domain.Manifests;

public sealed class ManifestDiffCalculator
{
    public ManifestDiff Calculate(SyncManifest local, SyncManifest? remote)
    {
        if (remote is null)
        {
            return new ManifestDiff(local.Items, Array.Empty<ManifestItem>(), Array.Empty<ManifestItem>());
        }

        Dictionary<string, ManifestItem> remoteItems = remote.Items.ToDictionary(Key, StringComparer.Ordinal);
        Dictionary<string, ManifestItem> localItems = local.Items.ToDictionary(Key, StringComparer.Ordinal);

        List<ManifestItem> missingRemote = [];
        List<ManifestItem> missingLocal = [];
        List<ManifestItem> diverged = [];

        foreach (ManifestItem item in local.Items)
        {
            if (!remoteItems.TryGetValue(Key(item), out ManifestItem? remoteItem))
            {
                missingRemote.Add(item);
                continue;
            }

            if (remoteItem.Version != item.Version || !StringComparer.Ordinal.Equals(remoteItem.Hash, item.Hash))
            {
                diverged.Add(item);
            }
        }

        foreach (ManifestItem item in remote.Items)
        {
            if (!localItems.ContainsKey(Key(item)))
            {
                missingLocal.Add(item);
            }
        }

        return new ManifestDiff(missingRemote, missingLocal, diverged);
    }

    private static string Key(ManifestItem item)
    {
        return $"{item.EntityType}:{item.EntityId:N}";
    }
}
