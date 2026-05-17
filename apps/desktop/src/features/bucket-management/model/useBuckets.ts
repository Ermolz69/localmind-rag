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

  return {
    buckets,
    createBucket,
    error,
    isLoading,
    loadBuckets,
    name,
    selectedBucketId,
    setName,
    setSelectedBucketId,
  };
}
