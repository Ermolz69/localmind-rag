import type { PropsWithChildren } from "react";
import { useCallback, useEffect, useState } from "react";
import { listen } from "@tauri-apps/api/event";
import { setApiBaseUrl } from "@shared/api";
import { StartupScreen } from "./StartupScreen";
import {
  copyDiagnosticsToClipboard,
  getAppRuntimeInfo,
  openLogsFolder,
  restartLocalApi,
  type AppRuntimeInfo,
} from "./runtime";

export function AppBootstrap({ children }: PropsWithChildren) {
  const [runtimeInfo, setRuntimeInfo] = useState<AppRuntimeInfo | null>(null);
  const [limitedMode, setLimitedMode] = useState(false);

  const applyRuntimeInfo = useCallback((info: AppRuntimeInfo) => {
    setRuntimeInfo(info);
    if (info.localApiStatus === "Ready" && info.baseUrl) {
      setApiBaseUrl(info.baseUrl);
    }
  }, []);

  useEffect(() => {
    let disposed = false;

    void getAppRuntimeInfo().then((info) => {
      if (!disposed) {
        applyRuntimeInfo(info);
      }
    });

    const unlisten = listen<AppRuntimeInfo>(
      "local-api-status-changed",
      (event) => applyRuntimeInfo(event.payload),
    );

    return () => {
      disposed = true;
      void unlisten.then((dispose) => dispose());
    };
  }, [applyRuntimeInfo]);

  if (runtimeInfo?.localApiStatus === "Ready" || limitedMode) {
    return <>{children}</>;
  }

  return (
    <StartupScreen
      runtimeInfo={runtimeInfo}
      onRetry={() => void restartLocalApi().then(applyRuntimeInfo)}
      onOpenLogs={() => void openLogsFolder()}
      onCopyDiagnostics={() => void copyDiagnosticsToClipboard()}
      onContinue={() => setLimitedMode(true)}
      canContinue={
        runtimeInfo?.localApiStatus === "Failed" ||
        runtimeInfo?.localApiStatus === "Crashed"
      }
    />
  );
}
