import { useCallback, useEffect, useState } from "react";

import type { OperationData } from "@shared/contracts";
import { companionApi, getErrorMessage } from "@shared/api";

export type CompanionFileRoot =
  OperationData<"GetCompanionFileRoots">["roots"][number];
export type CompanionBrowse = OperationData<"BrowseCompanionFiles">;
export type CompanionFileAddResult = { success: boolean; message: string };

/**
 * Browses the folders the user allowed on the computer and adds chosen files to
 * LocalMind. The phone only ever sees inside allowed roots — never the whole
 * disk — and files are added by path, not downloaded to the phone.
 */
export function useCompanionFiles() {
  const [roots, setRoots] = useState<CompanionFileRoot[]>([]);
  const [current, setCurrent] = useState<CompanionBrowse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [addingPath, setAddingPath] = useState<string | null>(null);

  const loadRoots = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await companionApi.getFileRoots();
      setRoots(response.roots);
      setError(null);
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to load folders."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadRoots();
  }, [loadRoots]);

  const browse = useCallback(async (path: string) => {
    setIsLoading(true);
    try {
      const response = await companionApi.browseFiles(path);
      setCurrent(response);
      setError(null);
    } catch (exception) {
      setError(getErrorMessage(exception, "Unable to open this folder."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  const goToRoots = useCallback(() => {
    setCurrent(null);
    setError(null);
  }, []);

  const addFile = useCallback(
    async (path: string): Promise<CompanionFileAddResult> => {
      setAddingPath(path);
      try {
        await companionApi.addFile({ path });
        return {
          success: true,
          message: "Added to LocalMind. Indexing will start shortly.",
        };
      } catch (exception) {
        return {
          success: false,
          message: getErrorMessage(exception, "Could not add this file."),
        };
      } finally {
        setAddingPath(null);
      }
    },
    [],
  );

  return {
    roots,
    current,
    isLoading,
    error,
    addingPath,
    browse,
    goToRoots,
    addFile,
  };
}
