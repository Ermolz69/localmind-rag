import { useEffect, useState } from "react";

import { companionApi } from "@shared/api";

/**
 * Loads the lightweight companion info shown by the phone interface (currently
 * the computer name). Fails soft: when LocalApi is unreachable the name is null.
 */
export function useCompanionInfo() {
  const [computerName, setComputerName] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let active = true;

    void (async () => {
      try {
        const info = await companionApi.getInfo();
        if (active) {
          setComputerName(info.computerName);
        }
      } catch {
        if (active) {
          setComputerName(null);
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    })();

    return () => {
      active = false;
    };
  }, []);

  return { computerName, isLoading };
}
