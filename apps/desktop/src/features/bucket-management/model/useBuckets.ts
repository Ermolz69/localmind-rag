import { useState } from "react";
import { bucketsApi } from "@shared/api";
import { useApiMutation, useApiQuery } from "@shared/lib/hooks";

export function useBuckets() {
  const [selectedBucketId, setSelectedBucketId] = useState("");
  const [name, setName] = useState("");

  const {
    data: buckets,
    isLoading,
    error: queryError,
    reload: loadBuckets,
  } = useApiQuery(() => bucketsApi.getBuckets(), {
    initialData: [],
    fallbackError: "Unable to load buckets.",
  });

  const createMutation = useApiMutation(
    (nextName: string) =>
      bucketsApi.createBucket({ name: nextName, description: null }),
    { fallbackError: "Bucket creation failed." },
  );

  const renameMutation = useApiMutation(
    (id: string, newName: string) =>
      bucketsApi.updateBucket(id, { name: newName, description: null }),
    { fallbackError: "Failed to rename bucket." },
  );

  const deleteMutation = useApiMutation(
    (id: string) => bucketsApi.deleteBucket(id),
    {
      fallbackError: "Failed to delete bucket.",
    },
  );

  async function createBucket() {
    const nextName = name.trim();
    if (!nextName) {
      return;
    }

    const bucket = await createMutation.mutate(nextName);
    if (bucket) {
      setName("");
      setSelectedBucketId(bucket.id);
      await loadBuckets();
    }
  }

  async function renameBucket(id: string, newName: string) {
    const nextName = newName.trim();
    if (!nextName) return;

    await renameMutation.mutate(id, nextName);
    await loadBuckets();
    setSelectedBucketId(id);
  }

  async function deleteBucket(id: string) {
    await deleteMutation.mutate(id);
    setSelectedBucketId((prev) => (prev === id ? "" : prev));
    await loadBuckets();
  }

  return {
    buckets: buckets ?? [],
    createBucket,
    renameBucket,
    deleteBucket,
    error:
      queryError ??
      createMutation.error ??
      renameMutation.error ??
      deleteMutation.error,
    isLoading:
      isLoading ||
      createMutation.isPending ||
      renameMutation.isPending ||
      deleteMutation.isPending,
    loadBuckets,
    name,
    selectedBucketId,
    setName,
    setSelectedBucketId,
  };
}
