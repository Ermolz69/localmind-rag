import type { PropsWithChildren } from "react";
import { useCallback, useEffect, useState } from "react";
import { listen } from "@tauri-apps/api/event";
import { setApiBaseUrl } from "@shared/api";
import { StartupScreen } from "./StartupScreen";
import {
  copyDiagnosticsToClipboard,
  enableLimitedMode,
  getAppRuntimeInfo,
  openLogsFolder,
  restartLocalApi,
  type AppRuntimeInfo,
} from "./runtime";

export function AppBootstrap({ children }: PropsWithChildren) {
  const [runtimeInfo, setRuntimeInfo] = useState<AppRuntimeInfo | null>(null);

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

    let unlistenClose: (() => void) | undefined;
    import("@tauri-apps/api/window").then(({ getCurrentWindow }) => {
      getCurrentWindow().onCloseRequested(async (event) => {
        event.preventDefault();
        const appWindow = getCurrentWindow();
        const timeout = setTimeout(() => void appWindow.destroy(), 4000);
        try {
          const { invoke } = await import("@tauri-apps/api/core");
          await invoke("shutdown_everything");
        } finally {
          clearTimeout(timeout);
          void appWindow.destroy();
        }
      }).then((unlistenCb) => {
        unlistenClose = unlistenCb;
      }).catch(() => {});
    }).catch(() => {});

    return () => {
      disposed = true;
      if (unlistenClose) {
        unlistenClose();
      }
      void unlisten.then((dispose) => dispose());
    };
  }, [applyRuntimeInfo]);

  if (
    runtimeInfo?.desktopMode === "Limited" ||
    runtimeInfo?.localApiStatus === "Ready"
  ) {
    return <>{children}</>;
  }

  return (
    <StartupScreen
      runtimeInfo={runtimeInfo}
      onRetry={() => void restartLocalApi()}
      onOpenLogs={() => void openLogsFolder()}
      onCopyDiagnostics={() => void copyDiagnosticsToClipboard()}
      onContinue={() => void enableLimitedMode().then(applyRuntimeInfo)}
      canContinue={
        runtimeInfo?.localApiStatus === "Failed" ||
        runtimeInfo?.localApiStatus === "Crashed"
      }
    />
  );
}
