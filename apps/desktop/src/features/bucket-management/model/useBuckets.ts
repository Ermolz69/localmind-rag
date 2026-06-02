import { useCallback, useEffect, useState } from "react";
import type { BucketDto } from "@entities/bucket";
import { bucketsApi, getErrorMessage } from "@shared/api";

export function useBuckets() {
  const [buckets, setBuckets] = useState<BucketDto[]>([]);
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [name, setName] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadBuckets = useCallback(async () => {
    setError(null);
    try {
      setBuckets(await bucketsApi.getBuckets());
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to load buckets."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadBuckets();
  }, [loadBuckets]);

  async function createBucket() {
    const nextName = name.trim();
    if (!nextName) {
      return;
    }

    setError(null);
    try {
      const bucket = await bucketsApi.createBucket({ name: nextName });
      setName("");
      setSelectedBucketId(bucket.id);
      await loadBuckets();
    } catch (exception) {
      setError(getErrorMessage(exception, "Bucket creation failed."));
    }
  }

  async function renameBucket(id: string, newName: string) {
    const nextName = newName.trim();
    if (!nextName) return;

    setError(null);
    try {
      await bucketsApi.updateBucket(id, { name: nextName });
      await loadBuckets();
      setSelectedBucketId(id);
    } catch (exception) {
      setError(getErrorMessage(exception, "Failed to rename bucket."));
    }
  }

  async function deleteBucket(id: string) {
    setError(null);
    try {
      await bucketsApi.deleteBucket(id);
      // if deleted bucket was selected, clear selection or pick first
      setSelectedBucketId((prev) => (prev === id ? "" : prev));
      await loadBuckets();
    } catch (exception) {
      setError(getErrorMessage(exception, "Failed to delete bucket."));
    }
  }

  return {
    buckets,
    createBucket,
    renameBucket,
    deleteBucket,
    error,
    isLoading,
    loadBuckets,
    name,
    selectedBucketId,
    setName,
    setSelectedBucketId,
  };
}
