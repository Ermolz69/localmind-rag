import type { OperationData, OperationJsonBody } from "@shared/contracts";
import { request } from "./http";

export const watchedFoldersApi = {
  getStatus: () =>
    request<OperationData<"GetWatchedFolderStatus">>("/watched-folders/status"),
  cleanup: () =>
    request<OperationData<"CleanupWatchedFolders">>(
      "/watched-folders/cleanup",
      {
        method: "POST",
      },
    ),
  rescan: (req: OperationJsonBody<"RescanWatchedFolders">) =>
    request<OperationData<"RescanWatchedFolders">>("/watched-folders/rescan", {
      method: "POST",
      body: JSON.stringify(req),
    }),
};
