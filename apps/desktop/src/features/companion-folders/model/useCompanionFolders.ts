import { useCallback, useEffect, useState } from "react";

import type { WatchedFolderStatusResponse } from "@entities/settings";
import { getErrorMessage, watchedFoldersApi } from "@shared/api";

export type CompanionFolderActionResult = {
  success: boolean;
  message: string;
};

/**
 * Manages the watched folders the user has already allowed on the computer: view
 * status, rescan, and clean up records of deleted files. By design it cannot add
 * new folders from the phone — it only acts on what the computer permits.
 */
export function useCompanionFolders() {
  const [status, setStatus] = useState<WatchedFolderStatusResponse | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rescanningPath, setRescanningPath] = useState<string | null>(null);
  const [isRescanningAll, setIsRescanningAll] = useState(false);
  const [isCleaning, setIsCleaning] = useState(false);

  const refresh = useCallback(async () => {
    try {
      const next = await watchedFoldersApi.getStatus();
      setStatus(next);
      setError(null);
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to load watched folders."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const rescan = useCallback(
    async (path?: string): Promise<CompanionFolderActionResult> => {
      if (path) {
        setRescanningPath(path);
      } else {
        setIsRescanningAll(true);
      }

      try {
        const response = await watchedFoldersApi.rescan({ path: path ?? null });
        const checked =
          Number(response.queuedCreatedOrChanged) +
          Number(response.unchangedFiles) +
          Number(response.unsupportedFiles);
        await refresh();
        return {
          success: true,
          message: `Rescan complete: ${checked} file(s) checked, ${response.queuedDeleted} missing.`,
        };
      } catch (exception) {
        return {
          success: false,
          message: getErrorMessage(exception, "Rescan failed."),
        };
      } finally {
        if (path) {
          setRescanningPath(null);
        } else {
          setIsRescanningAll(false);
        }
      }
    },
    [refresh],
  );

  const cleanup =
    useCallback(async (): Promise<CompanionFolderActionResult> => {
      setIsCleaning(true);
      try {
        const response = await watchedFoldersApi.cleanup();
        await refresh();
        return {
          success: true,
          message: `Cleaned ${response.cleanedCount} deleted document(s).`,
        };
      } catch (exception) {
        return {
          success: false,
          message: getErrorMessage(exception, "Cleanup failed."),
        };
      } finally {
        setIsCleaning(false);
      }
    }, [refresh]);

  return {
    status,
    isLoading,
    error,
    rescanningPath,
    isRescanningAll,
    isCleaning,
    refresh,
    rescan,
    cleanup,
  };
}
