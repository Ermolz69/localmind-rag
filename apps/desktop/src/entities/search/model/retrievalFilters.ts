import type { BucketDto } from "@entities/bucket";
import type { RetrievalFilters } from "./types";

export type SearchFilterKey =
  | "bucketId"
  | "date"
  | "fileType"
  | "documentId"
  | "tag";

export type SearchFilterChip = {
  key: SearchFilterKey;
  label: string;
  tagKey?: string;
};

export function hasActiveFilters(filters: RetrievalFilters): boolean {
  return Boolean(
    filters.bucketId ||
    filters.documentId ||
    filters.dateFrom ||
    filters.dateTo ||
    filters.fileType ||
    (filters.tags && Object.keys(filters.tags).length > 0),
  );
}

export function removeFilter(
  filters: RetrievalFilters,
  key: SearchFilterKey,
  tagKeyToRemove?: string,
): RetrievalFilters {
  if (key === "bucketId") {
    return { ...filters, bucketId: null };
  }

  if (key === "documentId") {
    return { ...filters, documentId: null };
  }

  if (key === "date") {
    return { ...filters, dateFrom: null, dateTo: null };
  }

  if (key === "fileType") {
    return { ...filters, fileType: null };
  }

  if (key === "tag" && filters.tags && tagKeyToRemove) {
    const newTags = { ...filters.tags };
    delete newTags[tagKeyToRemove];
    return { ...filters, tags: newTags };
  }

  return filters;
}

export function buildFilterChips(
  filters: RetrievalFilters,
  buckets: BucketDto[],
): SearchFilterChip[] {
  const chips: SearchFilterChip[] = [];

  if (filters.bucketId) {
    const bucket = buckets.find((item) => item.id === filters.bucketId);
    chips.push({
      key: "bucketId",
      label: `Bucket: ${bucket?.name ?? "Selected"}`,
    });
  }

  if (filters.documentId) {
    chips.push({
      key: "documentId",
      label: `Document`, // Could be improved if we pass documents list to resolve name, but we can't reliably resolve name from just ID if document isn't loaded. For now "Document" is fine, or we assume caller knows it. Actually we don't need the name if we don't have it, but chat UI might look weird. We will just use "Document".
    });
  }

  if (filters.dateFrom || filters.dateTo) {
    chips.push({
      key: "date",
      label: `Date: ${formatDateLabel(filters.dateFrom) || "Any"} to ${
        formatDateLabel(filters.dateTo) || "Any"
      }`,
    });
  }

  if (filters.fileType) {
    chips.push({
      key: "fileType",
      label: `File: ${filters.fileType}`,
    });
  }

  if (filters.tags) {
    for (const [tagKey, tagValue] of Object.entries(filters.tags)) {
      chips.push({
        key: "tag",
        label: `Tag: ${tagKey}=${tagValue}`,
        tagKey,
      });
    }
  }

  return chips;
}

function formatDateLabel(value?: string | null): string | null {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  const day = String(date.getUTCDate()).padStart(2, "0");
  const month = String(date.getUTCMonth() + 1).padStart(2, "0");
  return `${day}.${month}.${date.getUTCFullYear()}`;
}
