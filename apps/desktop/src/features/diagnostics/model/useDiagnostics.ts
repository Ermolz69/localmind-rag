import { useEffect, useMemo } from "react";
import type { DiagnosticsStatus } from "@entities/runtime";
import { diagnosticsApi } from "@shared/api";
import { useApiQuery } from "@shared/lib/hooks";

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
  const cached = useMemo(() => getCachedDiagnostics(Date.now()), []);
  const shouldRefresh = !cached || cached.state === "stale";

  const { data, isLoading, isFetching, error, reload } = useApiQuery(
    async () => {
      const diagnostics = await diagnosticsApi.getDiagnostics();
      writeDiagnosticsCache(diagnostics);
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
      writeDiagnosticsCache(data);
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

function getCachedDiagnostics(now: number) {
  const memory = readMemoryCache(now);
  if (memory) {
    return memory;
  }

  return readPersistedCache(now);
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

function readPersistedCache(now: number) {
  if (typeof window === "undefined") {
    return null;
  }

  try {
    const raw = window.localStorage.getItem(DIAGNOSTICS_CACHE_KEY);
    if (!raw) {
      return null;
    }

    const entry = JSON.parse(raw) as DiagnosticsCacheEntry;
    if (!isDiagnosticsCacheEntry(entry)) {
      window.localStorage.removeItem(DIAGNOSTICS_CACHE_KEY);
      return null;
    }

    const ageMs = now - entry.cachedAt;
    if (ageMs > DIAGNOSTICS_CACHE_POLICY.persistedMaxAgeMs) {
      window.localStorage.removeItem(DIAGNOSTICS_CACHE_KEY);
      return null;
    }

    memoryCache = entry;

    return {
      ...entry,
      state:
        ageMs <= DIAGNOSTICS_CACHE_POLICY.persistedFreshMs
          ? ("fresh" as const)
          : ("stale" as const),
    };
  } catch {
    window.localStorage.removeItem(DIAGNOSTICS_CACHE_KEY);
    return null;
  }
}

function writeDiagnosticsCache(data: DiagnosticsStatus) {
  const entry = { cachedAt: Date.now(), data };
  memoryCache = entry;

  if (typeof window === "undefined") {
    return;
  }

  try {
    window.localStorage.setItem(DIAGNOSTICS_CACHE_KEY, JSON.stringify(entry));
  } catch {
    // Diagnostics are optional; storage quota failures should not affect UI.
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
