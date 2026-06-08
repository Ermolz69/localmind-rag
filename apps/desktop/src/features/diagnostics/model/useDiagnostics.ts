import { useEffect, useMemo, useState } from "react";
import type { DiagnosticsStatus } from "@entities/runtime";
import { diagnosticsApi } from "@shared/api";
import { useApiQuery } from "@shared/lib/hooks";
import { readAppCache, writeAppCache } from "@app/startup/runtime";

const DIAGNOSTICS_CACHE_KEY = "localmind:diagnostics:v1";
const DIAGNOSTICS_CACHE_POLICY = {
  memoryFreshMs: 30_000,
  persistedFreshMs: 120_000,
  persistedMaxAgeMs: 10 * 60_000,
} as const;

type DiagnosticsCacheEntry = {
  cachedAt: number;
  data: DiagnosticsStatus;
};

let memoryCache: DiagnosticsCacheEntry | null = null;

export function useDiagnostics() {
  const [persistedCache, setPersistedCache] =
    useState<DiagnosticsCacheEntry | null>(null);

  useEffect(() => {
    if (!memoryCache) {
      void readPersistedCache(Date.now()).then((cache) => {
        if (cache) {
          setPersistedCache(cache);
        }
      });
    }
  }, []);

  const cached = useMemo(() => {
    const memory = readMemoryCache(Date.now());
    if (memory) {
      return memory;
    }

    if (persistedCache) {
      const ageMs = Date.now() - persistedCache.cachedAt;
      return {
        ...persistedCache,
        state:
          ageMs <= DIAGNOSTICS_CACHE_POLICY.persistedFreshMs
            ? ("fresh" as const)
            : ("stale" as const),
      };
    }
    return null;
  }, [persistedCache]);

  const shouldRefresh = !cached || cached.state === "stale";

  const { data, isLoading, isFetching, error, reload } = useApiQuery(
    async () => {
      const diagnostics = await diagnosticsApi.getDiagnostics();
      void writeDiagnosticsCache(diagnostics);
      return diagnostics;
    },
    {
      enabled: shouldRefresh,
      fallbackError: "Unable to load diagnostics.",
      initialData: cached?.data,
    },
  );

  useEffect(() => {
    if (data) {
      void writeDiagnosticsCache(data);
    }
  }, [data]);

  return {
    diagnostics: data ?? null,
    error,
    isLoading: isLoading && !cached,
    isRefreshing: isFetching && Boolean(data),
    lastUpdatedAt: cached?.cachedAt ?? memoryCache?.cachedAt ?? null,
    reload,
  };
}

function readMemoryCache(now: number) {
  if (!memoryCache) {
    return null;
  }

  const ageMs = now - memoryCache.cachedAt;
  if (ageMs <= DIAGNOSTICS_CACHE_POLICY.memoryFreshMs) {
    return { ...memoryCache, state: "fresh" as const };
  }

  return null;
}

async function readPersistedCache(now: number) {
  if (typeof window === "undefined") {
    return null;
  }

  try {
    const raw = await readAppCache(DIAGNOSTICS_CACHE_KEY);
    if (!raw) {
      return null;
    }

    const entry = JSON.parse(raw) as DiagnosticsCacheEntry;
    if (!isDiagnosticsCacheEntry(entry)) {
      void writeAppCache(DIAGNOSTICS_CACHE_KEY, "");
      return null;
    }

    const ageMs = now - entry.cachedAt;
    if (ageMs > DIAGNOSTICS_CACHE_POLICY.persistedMaxAgeMs) {
      void writeAppCache(DIAGNOSTICS_CACHE_KEY, "");
      return null;
    }

    memoryCache = entry;
    return entry;
  } catch {
    return null;
  }
}

async function writeDiagnosticsCache(data: DiagnosticsStatus) {
  const entry = { cachedAt: Date.now(), data };
  memoryCache = entry;

  if (typeof window === "undefined") {
    return;
  }

  try {
    await writeAppCache(DIAGNOSTICS_CACHE_KEY, JSON.stringify(entry));
  } catch {
    // Diagnostics are optional
  }
}

function isDiagnosticsCacheEntry(
  value: DiagnosticsCacheEntry,
): value is DiagnosticsCacheEntry {
  return (
    typeof value?.cachedAt === "number" &&
    typeof value.data === "object" &&
    value.data !== null &&
    Array.isArray(value.data.latestErrors)
  );
}
