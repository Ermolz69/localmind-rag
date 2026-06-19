const syncStatusLabels = new Map<number, string>([
  [0, "Local only"],
  [1, "Pending upload"],
  [2, "Uploading"],
  [3, "Uploaded"],
  [4, "Pending download"],
  [5, "Downloading"],
  [6, "Downloaded"],
  [7, "Synced"],
  [8, "Conflict"],
  [9, "Upload failed"],
  [10, "Download failed"],
  [11, "Deleted local"],
  [12, "Deleted remote"],
]);

export function formatBucketSyncStatus(syncStatus: number | string): string {
  if (typeof syncStatus === "number") {
    return syncStatusLabels.get(syncStatus) ?? `Unknown (${syncStatus})`;
  }

  const numericStatus = Number(syncStatus);
  if (Number.isInteger(numericStatus)) {
    return syncStatusLabels.get(numericStatus) ?? `Unknown (${syncStatus})`;
  }

  return syncStatus
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .trim()
    .replace(/\s+/g, " ")
    .replace(/^./, (character) => character.toUpperCase());
}
